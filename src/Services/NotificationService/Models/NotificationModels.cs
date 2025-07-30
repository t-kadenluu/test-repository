// Notification service models and implementations
namespace NotificationService.API.Models;

public enum NotificationType
{
    Email,
    SMS,
    PushNotification,
    InApp,
    Webhook
}

public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Urgent
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Delivered,
    Failed,
    Bounced,
    Cancelled
}

public class Notification
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; }
    public NotificationStatus Status { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? TemplateId { get; set; }
    public Dictionary<string, object> TemplateData { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public int RetryCount { get; set; }
    public string? FailureReason { get; set; }
    public string? ExternalId { get; set; }
    public string? Source { get; set; }
}

public class NotificationTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string? TextContent { get; set; }
    public List<string> RequiredVariables { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class EmailNotificationRequest
{
    public string To { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? HtmlContent { get; set; }
    public string? TextContent { get; set; }
    public string? TemplateId { get; set; }
    public Dictionary<string, object> TemplateData { get; set; } = new();
    public List<EmailAttachment> Attachments { get; set; } = new();
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public DateTime? ScheduledAt { get; set; }
    public string? Source { get; set; }
}

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}

public class SMSNotificationRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? TemplateId { get; set; }
    public Dictionary<string, object> TemplateData { get; set; } = new();
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public DateTime? ScheduledAt { get; set; }
    public string? Source { get; set; }
}

public class PushNotificationRequest
{
    public string DeviceToken { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public string? Icon { get; set; }
    public string? Sound { get; set; }
    public string? ClickAction { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public DateTime? ScheduledAt { get; set; }
    public string? Source { get; set; }
}

public class BulkNotificationRequest
{
    public NotificationType Type { get; set; }
    public List<string> Recipients { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? TemplateId { get; set; }
    public Dictionary<string, object> TemplateData { get; set; } = new();
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public DateTime? ScheduledAt { get; set; }
    public string BatchId { get; set; } = string.Empty;
    public string? Source { get; set; }
}

public class NotificationResponse
{
    public string Reference { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; }
    public string? ExternalId { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
}

public class BulkNotificationResponse
{
    public string BatchId { get; set; } = string.Empty;
    public int TotalNotifications { get; set; }
    public int SuccessfulNotifications { get; set; }
    public int FailedNotifications { get; set; }
    public List<NotificationResponse> Results { get; set; } = new();
    public DateTime ProcessedAt { get; set; }
}
