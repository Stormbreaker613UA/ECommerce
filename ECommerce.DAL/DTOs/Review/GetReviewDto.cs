namespace ECommerce.DAL.DTOs.Review
{
    public class GetReviewDto
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public Guid ProductId { get; set; }

        public int Rating { get; set; }

        public string? CommentText { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
