using System;
using System.Collections.Generic;

namespace ECommerce.DAL.DTOs.Product
{
    public class UpdateProductDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public int? StockQuantity { get; set; }
        public Guid? CategoryId { get; set; }
        public List<string>? ImageUrls { get; set; }
    }
}