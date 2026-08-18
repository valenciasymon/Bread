namespace Bread.API.Application.Dtos.AdminDto;

public record GetAdminDashBoardDto(
    GetAdminSummaryDto summary,
    List<GetUsersAdminDto> users,
    List<GetAuditLogsDto> audits);
