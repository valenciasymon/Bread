namespace Bread.API.Domain;

public class AuditLog
{
    public int AuditLogId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
