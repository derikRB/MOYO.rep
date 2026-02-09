using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Sego_and__Bux.Config;
using Sego_and__Bux.Data;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Services;
using Sego_and__Bux.Services.Interfaces;
using Sego_and__Bux.Hubs;

var builder = WebApplication.CreateBuilder(args);

// 1) Controllers + JSON cycles
builder.Services.AddControllers()
    .AddJsonOptions(o => {
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.WriteIndented = true;
    });

// 2) Swagger + JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
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
});

// 3) EF Core
builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 4) DI for services
builder.Services.AddScoped<IStockService, StockService>();
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

builder.Services.Configure<EmailJsSettings>(
    builder.Configuration.GetSection("EmailJsSettings")
);

// 5) Authentication & Authorization
var key = builder.Configuration["JwtSettings:Key"]!;
if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 16)
    throw new InvalidOperationException("JWT key too short");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => {
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
    });

builder.Services.AddAuthorization(opts => {
    opts.AddPolicy("CustomerOnly", p => p.RequireRole("Customer"));
    opts.AddPolicy("EmployeeOnly", p => p.RequireRole("Employee", "Manager", "Admin"));
    opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    opts.AddPolicy("ManagerOnly", p => p.RequireRole("Manager"));
    opts.AddPolicy("InventoryStaff", p => p.RequireRole("Employee", "Manager", "Admin"));
});

// 6) CORS for DEV: allow any origin and credentials (needed for SignalR, do not use in prod)
builder.Services.AddCors(o => o.AddPolicy("AllowAll", p =>
    p.SetIsOriginAllowed(_ => true)
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()
));

// 7) Build app
var app = builder.Build();

// 8) Ensure waybills folder exists for static files
var wb = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "waybills");
if (!Directory.Exists(wb)) Directory.CreateDirectory(wb);

// 8b) Ensure customizations folder exists for static files (uploaded images/snapshots)
var customizationsFolder = Path.Combine(app.Environment.WebRootPath, "customizations");
if (!Directory.Exists(customizationsFolder)) Directory.CreateDirectory(customizationsFolder);

// 9) Middleware pipeline
app.UseCors("AllowAll"); // Apply CORS globally (including static files)
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// --- Serve ALL static files (including /images, /images/products, etc) WITH CORS headers ---
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        ctx.Context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
    }
});

// For /waybills (PDF/image uploads)
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

// For /customizations (customer-uploaded images and Konva snapshots)
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

// 10) Map SignalR hub endpoint for Angular
app.MapHub<StockHub>("/stockHub");

// 11) Map controllers
app.MapControllers();

// 12) Swagger/dev tools
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.Run();
