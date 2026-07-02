using System;

namespace ECommerce.DAL.DTOs.Product
{
    public class GetProductDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        public Guid CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public List<string> ImageUrls { get; set; } = new();
    }
}