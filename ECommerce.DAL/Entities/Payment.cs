namespace ECommerce.DAL.Entities
{
    public class Payment : AuditableEntity
    {
        public Guid OrderId { get; set; }
        public Guid PaymentStatusId { get; set; }
        public Guid PaymentMethodId { get; set; }

        // Amount snapshot
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;

        // The creation key is supplied by the client and is immutable after creation.
        // It is nullable only for legacy rows created before the Payment foundation.
        public string? IdempotencyKey { get; set; }

        // A separate key makes completion retries idempotent without reusing the
        // creation key for a different operation.
        public string? CompletionIdempotencyKey { get; set; }

        // Gateway data
        public string? TransactionId { get; set; }  // from Stripe/LiqPay/WayForPay
        public string? GatewayResponse { get; set; }  // raw JSON response

        // Card snapshot
        public string? CardLast4 { get; set; }  // "4242"
        public string? CardBrand { get; set; }  // "Visa", "Mastercard"

        public DateTime? PaidAt { get; set; }    // set when status → Completed

        // Navigation
        public virtual Order Order { get; set; } = null!;
        public virtual Invoice? Invoice { get; set; }
        public virtual PaymentStatus PaymentStatus { get; set; } = null!;
        public virtual PaymentMethod PaymentMethod { get; set; } = null!;
    }
}
