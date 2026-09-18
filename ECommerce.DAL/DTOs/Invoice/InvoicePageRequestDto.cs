namespace ECommerce.DAL.DTOs.Invoice;

public sealed class InvoicePageRequestDto
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class InvoicePageResponseDto
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public IReadOnlyList<InvoiceResponseDto> Items { get; init; } = [];
}
