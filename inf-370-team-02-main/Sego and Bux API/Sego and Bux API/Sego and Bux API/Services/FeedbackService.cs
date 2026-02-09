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

            // Keep your SA time-zone write
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

            var cust = await _context.Customers.FindAsync(userId);

            return new FeedbackDto
            {
                FeedbackID = fb.FeedbackID,
                UserID = fb.UserID,
                OrderID = fb.OrderID,
                Rating = fb.Rating,
                Comments = fb.Comments,
                Recommend = fb.Recommend,
                SubmittedDate = fb.SubmittedDate,
                UserName = cust != null ? $"{cust.Username} {cust.Surname}" : null
            };
        }

        public Task<bool> HasGivenFeedbackAsync(int userId, int orderId)
            => _context.Feedbacks.AnyAsync(f => f.UserID == userId && f.OrderID == orderId);

        public Task<IEnumerable<FeedbackDto>> GetFeedbacksForOrderAsync(int orderId)
            => (from f in _context.Feedbacks
                join c in _context.Customers on f.UserID equals c.CustomerID
                where f.OrderID == orderId
                select new FeedbackDto
                {
                    FeedbackID = f.FeedbackID,
                    UserID = f.UserID,
                    OrderID = f.OrderID,
                    Rating = f.Rating,
                    Comments = f.Comments,
                    Recommend = f.Recommend,
                    SubmittedDate = f.SubmittedDate,
                    UserName = c.Username + " " + c.Surname
                })
                .ToListAsync()
                .ContinueWith(t => (IEnumerable<FeedbackDto>)t.Result);

        public Task<IEnumerable<FeedbackDto>> GetFeedbacksByUserAsync(int userId)
            => (from f in _context.Feedbacks
                join c in _context.Customers on f.UserID equals c.CustomerID
                where f.UserID == userId
                select new FeedbackDto
                {
                    FeedbackID = f.FeedbackID,
                    UserID = f.UserID,
                    OrderID = f.OrderID,
                    Rating = f.Rating,
                    Comments = f.Comments,
                    Recommend = f.Recommend,
                    SubmittedDate = f.SubmittedDate,
                    UserName = c.Username + " " + c.Surname
                })
                .ToListAsync()
                .ContinueWith(t => (IEnumerable<FeedbackDto>)t.Result);

        public Task<IEnumerable<FeedbackDto>> GetAllFeedbacksAsync()
            => (from f in _context.Feedbacks
                join c in _context.Customers on f.UserID equals c.CustomerID
                select new FeedbackDto
                {
                    FeedbackID = f.FeedbackID,
                    UserID = f.UserID,
                    OrderID = f.OrderID,
                    Rating = f.Rating,
                    Comments = f.Comments,
                    Recommend = f.Recommend,
                    SubmittedDate = f.SubmittedDate,
                    UserName = c.Username + " " + c.Surname
                })
                .ToListAsync()
                .ContinueWith(t => (IEnumerable<FeedbackDto>)t.Result);

        public Task<IEnumerable<FeedbackDto>> GetFeedbacksForProductAsync(int productId)
            => (from f in _context.Feedbacks
                join ol in _context.OrderLines on f.OrderID equals ol.OrderID
                join c in _context.Customers on f.UserID equals c.CustomerID
                where ol.ProductID == productId
                select new FeedbackDto
                {
                    FeedbackID = f.FeedbackID,
                    UserID = f.UserID,
                    OrderID = f.OrderID,
                    Rating = f.Rating,
                    Comments = f.Comments,
                    Recommend = f.Recommend,
                    SubmittedDate = f.SubmittedDate,
                    UserName = c.Username + " " + c.Surname
                })
                .Distinct()
                .ToListAsync()
                .ContinueWith(t => (IEnumerable<FeedbackDto>)t.Result);
    }
}
