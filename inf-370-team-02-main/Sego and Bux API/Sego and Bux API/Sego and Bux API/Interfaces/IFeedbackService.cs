using System.Collections.Generic;
using System.Threading.Tasks;
using Sego_and__Bux.DTOs;

namespace Sego_and__Bux.Interfaces
{
    public interface IFeedbackService
    {
        Task<FeedbackDto> AddFeedbackAsync(int userId, CreateFeedbackDto dto);
        Task<bool> HasGivenFeedbackAsync(int userId, int orderId);
        Task<IEnumerable<FeedbackDto>> GetFeedbacksForOrderAsync(int orderId);
        Task<IEnumerable<FeedbackDto>> GetFeedbacksByUserAsync(int userId);
        Task<IEnumerable<FeedbackDto>> GetAllFeedbacksAsync();

        // Feedback linked to orders containing a product
        Task<IEnumerable<FeedbackDto>> GetFeedbacksForProductAsync(int productId);
    }
}
