// Program.cs  (FULL FILE)
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Sego_and__Bux.Config;
using Sego_and__Bux.Data;
using Sego_and__Bux.Hubs;
using Sego_and__Bux.Infrastructure;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;
using Sego_and__Bux.Services;
using Sego_and__Bux.Services.Interfaces;
using InfraAuditWriter = Sego_and__Bux.Infrastructure.AuditWriter;
using Sego_and__Bux.Middleware; // <<< ADDED

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Controllers + JSON
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.WriteIndented = true;
    });

// Swagger + JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Sego & Bux API", Version = "v1" });

    var jwtDef = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };
    c.AddSecurityDefinition(jwtDef.Reference.Id, jwtDef);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { jwtDef, Array.Empty<string>() } });

    c.CustomSchemaIds(t => t.FullName);
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    c.MapType<IFormFile>(() => new OpenApiSchema { Type = "string", Format = "binary" });
});

// EF Core + interceptors
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<AuditSaveChangesInterceptor>();
builder.Services.AddSingleton<OrderLineSnapshotInterceptor>();

builder.Services.AddDbContext<ApplicationDbContext>((sp, o) =>
{
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    o.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                      sp.GetRequiredService<OrderLineSnapshotInterceptor>());
});

// DI
builder.Services.AddScoped(IoC => (IStockService)new StockService(
    IoC.GetRequiredService<ApplicationDbContext>(),
    IoC.GetRequiredService<IAuditWriter>()));

builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductTypeService, ProductTypeService>();
builder.Services.AddScoped<ICustomizationService, CustomizationService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IVatService, VatService>();
builder.Services.AddSingleton<EmailSender>();
builder.Services.AddScoped<IFaqService, FaqService>();
builder.Services.AddScoped<IChatbotConfigService, ChatbotConfigService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IProductReviewService, ProductReviewService>();
builder.Services.AddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();
builder.Services.AddSignalR();
builder.Services.AddScoped<IEmailService, EmailService>();

// Admin/config services
builder.Services.AddScoped<IAppConfigService, AppConfigService>();
builder.Services.AddScoped<ITimerService, TimerService>();
builder.Services.AddSingleton<IStorageService, StorageService>();
builder.Services.AddScoped<IMaintenanceService, MaintenanceServiceSqlServer>();
builder.Services.AddScoped<IAuditWriter, InfraAuditWriter>();

builder.Services.Configure<EmailJsSettings>(
    builder.Configuration.GetSection("EmailJsSettings"));

// Maintenance config/state
builder.Services.Configure<MaintenanceOptions>(builder.Configuration.GetSection("Maintenance"));
builder.Services.AddSingleton<MaintenanceState>();

// >>> Feature Access service (file-backed) <<<
// Registers the new dynamic feature access service
builder.Services.AddSingleton<IFeatureAccessService, FeatureAccessService>(); // <<< ADDED

// AuthN/Z
var key = builder.Configuration["JwtSettings:Key"]!;
if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 16)
    throw new InvalidOperationException("JWT key too short");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            ValidateLifetime = true
        };
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/metrics"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("CustomerOnly", p => p.RequireRole("Customer"));
    opts.AddPolicy("EmployeeOnly", p => p.RequireRole("Employee", "Manager", "Admin"));
    opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    opts.AddPolicy("ManagerOnly", p => p.RequireRole("Manager"));
    opts.AddPolicy("InventoryStaff", p => p.RequireRole("Employee", "Manager", "Admin"));
});

// CORS
builder.Services.AddCors(o => o.AddPolicy("AllowAll", p =>
    p.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials()
));

var app = builder.Build();

// --- Initialize maintenance state from config ---
{
    var opts = app.Services.GetRequiredService<IOptions<MaintenanceOptions>>().Value;
    var state = app.Services.GetRequiredService<MaintenanceState>();
    state.InitializeFrom(opts);
}

// --- Seed default StockReasons AFTER app is built ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (!await db.StockReasons.AnyAsync())
    {
        db.StockReasons.AddRange(new[]
        {
            new StockReason { Name = "Inventory count correction", SortOrder = 10 },
            new StockReason { Name = "Damaged / write-off",       SortOrder = 20 },
            new StockReason { Name = "Expired",                    SortOrder = 30 },
            new StockReason { Name = "Lost",                       SortOrder = 40 },
            new StockReason { Name = "Stolen",                     SortOrder = 50 },
            new StockReason { Name = "Supplier return",            SortOrder = 60 },
            new StockReason { Name = "Customer return to shelf",   SortOrder = 70 },
            new StockReason { Name = "Found during count",         SortOrder = 80 },
            new StockReason { Name = "Promo sample / freebie",     SortOrder = 90 },
            new StockReason { Name = "Repack / bundle",            SortOrder = 100 },
            new StockReason { Name = "Transfer in",                SortOrder = 110 },
            new StockReason { Name = "Transfer out",               SortOrder = 120 },
            new StockReason { Name = "Resupplied",                 SortOrder = 130 }
        });
        await db.SaveChangesAsync();
    }
}

// dirs
var wb = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "waybills");
if (!Directory.Exists(wb)) Directory.CreateDirectory(wb);
var customizationsFolder = Path.Combine(app.Environment.WebRootPath, "customizations");
if (!Directory.Exists(customizationsFolder)) Directory.CreateDirectory(customizationsFolder);

// pipeline
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// maintenance (after auth so role bypass works)
app.UseMiddleware<MaintenanceModeMiddleware>();

// >>> Feature access middleware (AFTER auth/authorization) <<<
app.UseMiddleware<FeatureAccessMiddleware>(); // <<< ADDED

// extra static
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        ctx.Context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
    }
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(wb),
    RequestPath = "/waybills",
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        ctx.Context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
    }
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(customizationsFolder),
    RequestPath = "/customizations",
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        ctx.Context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
    }
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sego and Bux v1");
    c.RoutePrefix = "swagger";
});

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
app.UseServerTimeHeader();

app.MapHub<StockHub>("/stockHub");
app.MapHub<MetricsHub>("/hubs/metrics");
app.MapControllers();
app.Run();
