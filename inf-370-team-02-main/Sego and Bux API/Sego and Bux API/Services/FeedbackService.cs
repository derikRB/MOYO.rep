using Sego_and__Bux.Data;
using Sego_and__Bux.DTOs;
using Sego_and__Bux.Interfaces;
using Sego_and__Bux.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sego_and__Bux.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly ApplicationDbContext _context;
        public FeedbackService(ApplicationDbContext context) => _context = context;

        public async Task<FeedbackDto> AddFeedbackAsync(int userId, CreateFeedbackDto dto)
        {
            if (await _context.Feedbacks.AnyAsync(f => f.UserID == userId && f.OrderID == dto.OrderID))
                throw new InvalidOperationException("Feedback for this order already exists.");

            // → Convert UTC now into South Africa Standard Time (UTC+2)
            // Windows Zone ID: "South Africa Standard Time"
            // On Linux: "Africa/Johannesburg"
            var saZone = TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time");
            var submittedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, saZone);

            var fb = new Feedback
            {
                UserID = userId,
                OrderID = dto.OrderID,
                Rating = dto.Rating,
                Comments = dto.Comments,
                Recommend = dto.Recommend,
                SubmittedDate = submittedAt
            };

            _context.Feedbacks.Add(fb);
            await _context.SaveChangesAsync();

            return new FeedbackDto
            {
                FeedbackID = fb.FeedbackID,
                UserID = fb.UserID,
                OrderID = fb.OrderID,
                Rating = fb.Rating,
                Comments = fb.Comments,
                Recommend = fb.Recommend,
                SubmittedDate = fb.SubmittedDate
            };
        }

        public Task<bool> HasGivenFeedbackAsync(int userId, int orderId)
            => _context.Feedbacks.AnyAsync(f => f.UserID == userId && f.OrderID == orderId);

        public Task<IEnumerable<FeedbackDto>> GetFeedbacksForOrderAsync(int orderId)
            => _context.Feedbacks
                .Where(f => f.OrderID == orderId)
                .Select(f => new FeedbackDto
                {
                    FeedbackID = f.FeedbackID,
                    UserID = f.UserID,
                    OrderID = f.OrderID,
                    Rating = f.Rating,
                    Comments = f.Comments,
                    Recommend = f.Recommend,
                    SubmittedDate = f.SubmittedDate
                })
                .ToListAsync()
                .ContinueWith(t => (IEnumerable<FeedbackDto>)t.Result);

        public Task<IEnumerable<FeedbackDto>> GetFeedbacksByUserAsync(int userId)
            => _context.Feedbacks
                .Where(f => f.UserID == userId)
                .Select(f => new FeedbackDto
                {
                    FeedbackID = f.FeedbackID,
                    UserID = f.UserID,
                    OrderID = f.OrderID,
                    Rating = f.Rating,
                    Comments = f.Comments,
                    Recommend = f.Recommend,
                    SubmittedDate = f.SubmittedDate
                })
                .ToListAsync()
                .ContinueWith(t => (IEnumerable<FeedbackDto>)t.Result);

        public Task<IEnumerable<FeedbackDto>> GetAllFeedbacksAsync()
            => _context.Feedbacks
                .Select(f => new FeedbackDto
                {
                    FeedbackID = f.FeedbackID,
                    UserID = f.UserID,
                    OrderID = f.OrderID,
                    Rating = f.Rating,
                    Comments = f.Comments,
                    Recommend = f.Recommend,
                    SubmittedDate = f.SubmittedDate
                })
                .ToListAsync()
                .ContinueWith(t => (IEnumerable<FeedbackDto>)t.Result);
    }
}

