namespace ECommerce.DAL.DTOs.Order;

public class GetOrderDto
{
    public Guid Id { get; set; }

    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = string.Empty;

    public List<GetOrderItemDto> Items { get; set; } = new();
}