namespace Bread.API.Domain;

public class AuditLog
{
    public int AuditLogId { get; set; }
    public required string UserId { get; set; }

    public required string Action { get; set; }
    public required string Details { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
