namespace ECommerce.DAL.DTOs.Review
{
    public class AddReviewDto
    {
        public Guid ProductId { get; set; }
        public int Rating { get; set; }
        public string? CommentText { get; set; }
    }
}
