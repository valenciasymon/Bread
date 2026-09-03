namespace Bread.API.Application.Dtos.AdminDto;

public record GetAuditLogsDto(
    int AuditLogId,
    string FullName,
    string Role,
    string Action,
    string Description,
    DateTime CreatedAtUtc);