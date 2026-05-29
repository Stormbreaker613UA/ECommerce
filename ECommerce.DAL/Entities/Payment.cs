namespace ECommerce.DAL.Entities
{
    public class Payment : AuditableEntity
    {
        public Guid OrderId { get; set; }
        public Guid PaymentStatusId { get; set; }
        public Guid PaymentMethodId { get; set; }

        // Amount snapshot
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";

        // Gateway data
        public string? TransactionId { get; set; }  // from Stripe/LiqPay/WayForPay
        public string? GatewayResponse { get; set; }  // raw JSON response

        // Card snapshot
        public string? CardLast4 { get; set; }  // "4242"
        public string? CardBrand { get; set; }  // "Visa", "Mastercard"

        public DateTime? PaidAt { get; set; }    // set when status → Completed

        // Navigation
        public virtual Order Order { get; set; } = null!;
        public virtual PaymentStatus PaymentStatus { get; set; } = null!;
        public virtual PaymentMethod PaymentMethod { get; set; } = null!;
    }
}
