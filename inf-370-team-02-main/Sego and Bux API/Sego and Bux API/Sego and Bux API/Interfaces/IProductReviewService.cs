using System.Collections.Generic;
using System.Threading.Tasks;
using Sego_and__Bux.DTOs;

namespace Sego_and__Bux.Interfaces
{
    public interface IProductReviewService
    {
        Task<ProductReviewDto> AddReviewAsync(int userId, CreateProductReviewDto dto);
        Task<bool> HasReviewedAsync(int userId, int orderId, int productId);
        Task<List<ProductReviewDto>> GetPendingReviewsAsync();
        Task ApproveReviewAsync(int reviewId);
        Task DeclineReviewAsync(int reviewId);
        Task<List<ProductReviewDto>> GetApprovedReviewsByProductAsync(int productId);

        Task<List<ProductReviewDto>> GetReviewsByStatusAsync(string status);

        Task BulkApproveAsync(int[] reviewIds);
        Task BulkDeclineAsync(int[] reviewIds);

        Task<PagedResult<ProductReviewDto>> GetAllReviewsPagedAsync(int page, int pageSize);
    }

    public class PagedResult<T>
    {
        public int TotalCount { get; set; }
        public List<T> Items { get; set; } = new();
    }
}
