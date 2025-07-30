// Payment processing models and services
namespace PaymentGateway.API.Models;

public enum PaymentMethod
{
    CreditCard,
    DebitCard,
    PayPal,
    ApplePay,
    GooglePay,
    BankTransfer,
    Cryptocurrency
}

public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled,
    Refunded,
    Disputed
}

public class Payment
{
    public int Id { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
    public string? ExternalTransactionId { get; set; }
    public PaymentDetails? Details { get; set; }
}

public class PaymentDetails
{
    public string? CardLast4 { get; set; }
    public string? CardType { get; set; }
    public string? PayPalTransactionId { get; set; }
    public string? BankAccountLast4 { get; set; }
    public string? CryptoWalletAddress { get; set; }
    public Dictionary<string, string> AdditionalData { get; set; } = new();
}

public class PaymentRequest
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentMethod Method { get; set; }
    public CreditCardInfo? CreditCard { get; set; }
    public PayPalInfo? PayPal { get; set; }
    public BankTransferInfo? BankTransfer { get; set; }
    public string? CallbackUrl { get; set; }
}

public class CreditCardInfo
{
    public string CardNumber { get; set; } = string.Empty;
    public string ExpiryMonth { get; set; } = string.Empty;
    public string ExpiryYear { get; set; } = string.Empty;
    public string CVV { get; set; } = string.Empty;
    public string CardHolderName { get; set; } = string.Empty;
    public BillingAddress? BillingAddress { get; set; }
}

public class PayPalInfo
{
    public string PayPalEmail { get; set; } = string.Empty;
    public string? PayPalUserId { get; set; }
}

public class BankTransferInfo
{
    public string AccountNumber { get; set; } = string.Empty;
    public string RoutingNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
}

public class BillingAddress
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public class PaymentResponse
{
    public string PaymentReference { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public string? ExternalTransactionId { get; set; }
    public string? FailureReason { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string? RedirectUrl { get; set; }
    public decimal ProcessingFee { get; set; }
}

public class RefundRequest
{
    public string PaymentReference { get; set; } = string.Empty;
    public decimal? Amount { get; set; } // Null for full refund
    public string Reason { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public class RefundResponse
{
    public string RefundReference { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string? ExternalRefundId { get; set; }
}
