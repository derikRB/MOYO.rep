using System;
using Microsoft.EntityFrameworkCore;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Models;

namespace Sego_and__Bux.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // Core Entities
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductType> ProductTypes { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderLine> OrderLines { get; set; } = null!;
        public DbSet<Customization> Customizations { get; set; }
        public DbSet<OrderStatus> OrderStatuses { get; set; } = null!;
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }
        public DbSet<ProductTemplate> ProductTemplates { get; set; }
        public DbSet<Vat> Vats { get; set; }
        public DbSet<Template> Templates { get; set; }

        // Stock Management
        public DbSet<StockPurchase> StockPurchases { get; set; }
        public DbSet<StockPurchaseLine> StockPurchaseLines { get; set; }
        public DbSet<StockReceipt> StockReceipts { get; set; }
        public DbSet<StockReceiptLine> StockReceiptLines { get; set; }
        public DbSet<StockAdjustment> StockAdjustments { get; set; }
        public DbSet<LowStockAlert> LowStockAlerts { get; set; }

        // For writing to StockTransactions table
        public DbSet<StockTransactionEntity> StockTransactionEntities { get; set; } // actual table
                                                                                    // For reading from vw_StockTransactions view
        public DbSet<StockTransaction> StockTransactionReports { get; set; }        // reporting view

        // FAQ/Chatbot
        public DbSet<FaqItem> FaqItems { get; set; }
        public DbSet<ChatbotConfig> ChatbotConfigs { get; set; }

        // Reporting Views
        public DbSet<SalesReport> SalesReports { get; set; }
        public DbSet<OrderReport> OrderReports { get; set; }
        public DbSet<FinancialReport> FinancialReports { get; set; }
        public DbSet<StockForecast> StockForecasts { get; set; }
        public DbSet<StockReport> StockReports { get; set; }
        public DbSet<OrdersByCustomer> OrdersByCustomers { get; set; }
        public DbSet<FinancialReportByPeriod> FinancialReportsByPeriod { get; set; }

        // Feedback
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Reporting view mappings (keyless)
            modelBuilder.Entity<SalesReport>()
                        .HasNoKey()
                        .ToView("vw_SalesReport");

            modelBuilder.Entity<OrderReport>()
                        .HasNoKey()
                        .ToView("vw_OrderReport");

            modelBuilder.Entity<FinancialReport>()
                        .HasNoKey()
                        .ToView("vw_FinancialReport");

            modelBuilder.Entity<StockForecast>()
                        .HasNoKey()
                        .ToView("vw_StockForecast");

            modelBuilder.Entity<StockReport>()
                        .HasNoKey()
                        .ToView("vw_StockReport");

            modelBuilder.Entity<OrdersByCustomer>()
                        .HasNoKey()
                        .ToView("vw_OrdersByCustomer");

            // This is the DTO/view mapping (keyless)
            modelBuilder.Entity<StockTransactionEntity>()
          .ToTable("StockTransactions")
          .HasKey(st => st.StockTransactionID);

            // Reporting view (keyless)
            modelBuilder.Entity<StockTransaction>()
                .HasNoKey()
                .ToView("vw_StockTransactions");

            modelBuilder.Entity<FinancialReportByPeriod>()
                        .HasNoKey()
                        .ToView("vw_FinancialReport_ByPeriod");

            // --- Relationships (unchanged) ---
            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderLines)
                .WithOne(ol => ol.Order)
                .HasForeignKey(ol => ol.OrderID);

            modelBuilder.Entity<OrderLine>()
                .HasOne(ol => ol.Customization)
                .WithOne(c => c.OrderLine)
                .HasForeignKey<Customization>(c => c.OrderLineID);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.OrderStatus)
                .WithMany(s => s.Orders)
                .HasForeignKey(o => o.OrderStatusID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerID);

            modelBuilder.Entity<ProductType>()
                .HasOne(pt => pt.Category)
                .WithMany(c => c.ProductTypes)
                .HasForeignKey(pt => pt.CategoryID);

            modelBuilder.Entity<Product>()
                 .HasMany(p => p.ProductImages)
                 .WithOne(pi => pi.Product)
                 .HasForeignKey(pi => pi.ProductID);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.PrimaryImage)
                .WithMany()
                .HasForeignKey(p => p.PrimaryImageID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ProductTemplate>()
                .HasKey(pt => new { pt.ProductID, pt.TemplateID });
            modelBuilder.Entity<ProductTemplate>()
                .HasOne(pt => pt.Product)
                .WithMany(p => p.ProductTemplates)
                .HasForeignKey(pt => pt.ProductID);
            modelBuilder.Entity<ProductTemplate>()
                .HasOne(pt => pt.Template)
                .WithMany(t => t.ProductTemplates)
                .HasForeignKey(pt => pt.TemplateID);

            modelBuilder.Entity<StockPurchase>()
                  .HasMany(sp => sp.Lines)
                  .WithOne(l => l.StockPurchase)
                  .HasForeignKey(l => l.StockPurchaseId)
                  .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StockReceipt>()
                .HasOne(sr => sr.StockPurchase)
                .WithMany(sp => sp.Receipts)
                .HasForeignKey(sr => sr.StockPurchaseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StockReceipt>()
                .HasMany(sr => sr.Lines)
                .WithOne(sl => sl.StockReceipt)
                .HasForeignKey(sl => sl.StockReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StockReceipt>()
                .HasIndex(sr => sr.ReceiptDate);

            modelBuilder.Entity<StockAdjustment>()
                .HasOne(sa => sa.Product)
                .WithMany()
                .HasForeignKey(sa => sa.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockAdjustment>()
                .HasIndex(sa => sa.AdjustmentDate);

            modelBuilder.Entity<LowStockAlert>()
                .HasOne(a => a.Product)
                .WithMany()
                .HasForeignKey(a => a.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LowStockAlert>()
                .HasIndex(a => a.AlertDate);

            // --- VAT configuration & seed ---
            modelBuilder.Entity<Vat>()
                        .ToTable("VAT")
                        .HasKey(v => v.VatId);
            modelBuilder.Entity<Vat>()
                        .Property(v => v.Percentage)
                        .HasPrecision(5, 2);

            modelBuilder.Entity<Vat>().HasData(new Vat
            {
                VatId = 1,
                VatName = "Standard 15%",
                Percentage = 15.00m,
                EffectiveDate = new DateTime(2025, 7, 1),
                Status = "Active",
                CreatedAt = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc)
            });

            modelBuilder.Entity<OrderStatus>().HasData(
                new OrderStatus { OrderStatusID = 1, OrderStatusName = "Pending", Description = "Awaiting processing" },
                new OrderStatus { OrderStatusID = 2, OrderStatusName = "Processing", Description = "Packing your items" },
                new OrderStatus { OrderStatusID = 3, OrderStatusName = "Shipped", Description = "Out with courier" },
                new OrderStatus { OrderStatusID = 4, OrderStatusName = "Delivered", Description = "Delivered to customer" }
            );

            modelBuilder.Entity<UserActivityLog>()
                        .HasKey(l => l.AuditID);

            modelBuilder.Entity<FaqItem>()
                        .HasKey(f => f.FaqId);
            modelBuilder.Entity<FaqItem>()
                        .Property(f => f.SortOrder)
                        .HasDefaultValue(0);

            modelBuilder.Entity<ChatbotConfig>().HasData(new ChatbotConfig
            {
                Id = 1,
                WhatsAppNumber = "0653819207",
                SupportEmail = "qwertify2025@gmail.com"
            });

            modelBuilder.Entity<Feedback>()
                        .HasIndex(f => new { f.UserID, f.FeedbackID })
                        .IsUnique();

            modelBuilder.Entity<ProductReview>()
                        .Property(r => r.Status)
                        .HasDefaultValue("Pending");
        }
    }
}
