// DTOs and Request/Response models
namespace OrderProcessing.API.DTOs;

public class OrderSummaryDto
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int PendingOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public DateTime ReportDate { get; set; }
}

public class OrderFilterRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public OrderStatus? Status { get; set; }
    public int? UserId { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "CreatedAt";
    public string SortDirection { get; set; } = "desc";
}

public class OrderStatisticsResponse
{
    public Dictionary<string, int> OrdersByStatus { get; set; } = new();
    public Dictionary<string, decimal> RevenueByMonth { get; set; } = new();
    public List<TopCustomer> TopCustomers { get; set; } = new();
    public List<PopularProduct> PopularProducts { get; set; } = new();
}

public class TopCustomer
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
}

public class PopularProduct
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public int TotalQuantity { get; set; }
}

public class BulkOrderRequest
{
    public List<CreateOrderRequest> Orders { get; set; } = new();
    public string BatchId { get; set; } = string.Empty;
    public string ProcessedBy { get; set; } = string.Empty;
}

public class BulkOrderResponse
{
    public string BatchId { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public int SuccessfulOrders { get; set; }
    public int FailedOrders { get; set; }
    public List<OrderProcessingError> Errors { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
}

public class OrderProcessingError
{
    public int OrderIndex { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
}
