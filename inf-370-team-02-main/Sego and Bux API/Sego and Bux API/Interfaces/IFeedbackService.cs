using Sego_and__Bux.DTOs;
using Sego_and__Bux.Models;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Sego_and__Bux.Interfaces
{
    public interface IFeedbackService
    {
        Task<FeedbackDto> AddFeedbackAsync(int userId, CreateFeedbackDto dto);
        Task<bool> HasGivenFeedbackAsync(int userId, int orderId);
        Task<IEnumerable<FeedbackDto>> GetFeedbacksForOrderAsync(int orderId);
        Task<IEnumerable<FeedbackDto>> GetFeedbacksByUserAsync(int userId);
        Task<IEnumerable<FeedbackDto>> GetAllFeedbacksAsync();
    }
}
