using System;

namespace ECommerce.DAL.DTOs.Review
{
    public class UpdateReviewDto
    {
        public int Rating { get; set; }
        public string? CommentText { get; set; }
    }
}
