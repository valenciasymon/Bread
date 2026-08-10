namespace Bread.API.Application.Dtos.AdminDto;

public record GetAuditLogsDto(
    string FullName,
    string Action,
    string Description,
    DateOnly CreatedAtUtc);