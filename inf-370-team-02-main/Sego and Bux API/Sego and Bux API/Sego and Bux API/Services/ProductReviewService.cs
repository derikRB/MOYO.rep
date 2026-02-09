using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Sego_and__Bux.Data;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sego_and__Bux.Services
{
    public class ProductReviewService : IProductReviewService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductReviewService(ApplicationDbContext ctx, IWebHostEnvironment env)
        {
            _context = ctx;
            _env = env;
        }

        public async Task<ProductReviewDto> AddReviewAsync(int userId, CreateProductReviewDto dto)
        {
            if (await HasReviewedAsync(userId, dto.OrderID, dto.ProductID))
                throw new InvalidOperationException("Already reviewed");

            string? photoFileName = null;
            if (dto.Photo != null)
            {
                photoFileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.Photo.FileName)}";
                var savePath = Path.Combine(_env.WebRootPath, "reviews", photoFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
                await using var stream = new FileStream(savePath, FileMode.Create);
                await dto.Photo.CopyToAsync(stream);
            }

            var review = new ProductReview
            {
                UserID = userId,
                ProductID = dto.ProductID,
                OrderID = dto.OrderID,
                Rating = dto.Rating,
                ReviewTitle = dto.ReviewTitle,
                ReviewText = dto.ReviewText,
                PhotoFileName = photoFileName,
                SubmittedDate = DateTime.UtcNow,
                Status = "Pending"
            };
            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();

            var customer = await _context.Customers.FindAsync(userId);
            var product = await _context.Products.FindAsync(dto.ProductID);

            return new ProductReviewDto
            {
                ReviewID = review.ReviewID,
                ProductID = review.ProductID,
                UserID = review.UserID,
                OrderID = review.OrderID,
                Rating = review.Rating,
                ReviewTitle = review.ReviewTitle,
                ReviewText = review.ReviewText,
                PhotoUrl = photoFileName != null ? $"/reviews/{photoFileName}" : null,
                SubmittedDate = review.SubmittedDate,
                Status = review.Status,
                UserName = $"{customer?.Username} {customer?.Surname}".Trim(),
                ProductName = product?.Name
            };
        }

        public Task<bool> HasReviewedAsync(int userId, int orderId, int productId)
            => _context.ProductReviews.AnyAsync(r => r.UserID == userId && r.OrderID == orderId && r.ProductID == productId);

        public async Task<List<ProductReviewDto>> GetPendingReviewsAsync()
        {
            return await _context.ProductReviews
                .Include(r => r.Customer)
                .Include(r => r.Product).ThenInclude(p => p.ProductImages)
                .Where(r => r.Status == "Pending")
                .OrderByDescending(r => r.SubmittedDate)
                .Select(r => new ProductReviewDto
                {
                    ReviewID = r.ReviewID,
                    ProductID = r.ProductID,
                    UserID = r.UserID,
                    OrderID = r.OrderID,
                    Rating = r.Rating,
                    ReviewTitle = r.ReviewTitle,
                    ReviewText = r.ReviewText,
                    PhotoUrl = r.PhotoFileName != null ? $"/reviews/{r.PhotoFileName}" : null,
                    SubmittedDate = r.SubmittedDate,
                    Status = r.Status,
                    UserName = r.Customer.Username + " " + r.Customer.Surname,
                    ProductName = r.Product.Name,
                    ProductImageUrl = r.Product.ProductImages.FirstOrDefault() != null
                        ? "/images/products/" + r.Product.ProductImages.First()!.ImagePath
                        : null
                })
                .ToListAsync();
        }

        public async Task ApproveReviewAsync(int reviewId)
        {
            var review = await _context.ProductReviews.FindAsync(reviewId);
            if (review != null)
            {
                review.Status = "Approved";
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeclineReviewAsync(int reviewId)
        {
            var review = await _context.ProductReviews.FindAsync(reviewId);
            if (review != null)
            {
                review.Status = "Declined";
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<ProductReviewDto>> GetApprovedReviewsByProductAsync(int productId)
        {
            return await _context.ProductReviews
                .Include(r => r.Customer)
                .Include(r => r.Product).ThenInclude(p => p.ProductImages)
                .Where(r => r.Status == "Approved" && r.ProductID == productId)
                .OrderByDescending(r => r.SubmittedDate)
                .Select(r => new ProductReviewDto
                {
                    ReviewID = r.ReviewID,
                    ProductID = r.ProductID,
                    UserID = r.UserID,
                    OrderID = r.OrderID,
                    Rating = r.Rating,
                    ReviewTitle = r.ReviewTitle,
                    ReviewText = r.ReviewText,
                    PhotoUrl = r.PhotoFileName != null ? $"/reviews/{r.PhotoFileName}" : null,
                    SubmittedDate = r.SubmittedDate,
                    Status = r.Status,
                    UserName = r.Customer.Username + " " + r.Customer.Surname,
                    ProductName = r.Product.Name,
                    ProductImageUrl = r.Product.ProductImages.FirstOrDefault() != null
                        ? "/images/products/" + r.Product.ProductImages.First()!.ImagePath
                        : null
                })
                .ToListAsync();
        }

        public async Task<List<ProductReviewDto>> GetReviewsByStatusAsync(string status)
        {
            return await _context.ProductReviews
                .Include(r => r.Customer)
                .Include(r => r.Product).ThenInclude(p => p.ProductImages)
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.SubmittedDate)
                .Select(r => new ProductReviewDto
                {
                    ReviewID = r.ReviewID,
                    ProductID = r.ProductID,
                    UserID = r.UserID,
                    OrderID = r.OrderID,
                    Rating = r.Rating,
                    ReviewTitle = r.ReviewTitle,
                    ReviewText = r.ReviewText,
                    PhotoUrl = r.PhotoFileName != null ? $"/reviews/{r.PhotoFileName}" : null,
                    SubmittedDate = r.SubmittedDate,
                    Status = r.Status,
                    UserName = r.Customer.Username + " " + r.Customer.Surname,
                    ProductName = r.Product.Name,
                    ProductImageUrl = r.Product.ProductImages.FirstOrDefault() != null
                        ? "/images/products/" + r.Product.ProductImages.First()!.ImagePath
                        : null
                })
                .ToListAsync();
        }

        public async Task BulkApproveAsync(int[] reviewIds)
        {
            var reviews = await _context.ProductReviews.Where(r => reviewIds.Contains(r.ReviewID)).ToListAsync();
            foreach (var r in reviews) r.Status = "Approved";
            await _context.SaveChangesAsync();
        }

        public async Task BulkDeclineAsync(int[] reviewIds)
        {
            var reviews = await _context.ProductReviews.Where(r => reviewIds.Contains(r.ReviewID)).ToListAsync();
            foreach (var r in reviews) r.Status = "Declined";
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResult<ProductReviewDto>> GetAllReviewsPagedAsync(int page, int pageSize)
        {
            var total = await _context.ProductReviews.CountAsync();

            var items = await _context.ProductReviews
                .Include(r => r.Customer)
                .Include(r => r.Product).ThenInclude(p => p.ProductImages)
                .OrderByDescending(r => r.SubmittedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ProductReviewDto
                {
                    ReviewID = r.ReviewID,
                    ProductID = r.ProductID,
                    UserID = r.UserID,
                    OrderID = r.OrderID,
                    Rating = r.Rating,
                    ReviewTitle = r.ReviewTitle,
                    ReviewText = r.ReviewText,
                    PhotoUrl = r.PhotoFileName != null ? $"/reviews/{r.PhotoFileName}" : null,
                    SubmittedDate = r.SubmittedDate,
                    Status = r.Status,
                    UserName = r.Customer.Username + " " + r.Customer.Surname,
                    ProductName = r.Product.Name,
                    ProductImageUrl = r.Product.ProductImages.FirstOrDefault() != null
                        ? "/images/products/" + r.Product.ProductImages.First()!.ImagePath
                        : null
                })
                .ToListAsync();

            return new PagedResult<ProductReviewDto> { TotalCount = total, Items = items };
        }
    }
}
