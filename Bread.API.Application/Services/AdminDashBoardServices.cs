using Bread.API.Application.Contracts;
using Bread.API.Application.Dtos.AdminDto;
using Bread.API.Common.Constants;
using Bread.API.Common.Results;
using Bread.API.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Bread.API.Application.Services;

public class AdminDashBoardServices(
    IHttpContextAccessor httpContext,
    BreadDbContext breadContext,
    AuthDbContext authContext) : IAdminDashBoardServices
{
    private string adminId =
        httpContext.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? string.Empty;

    private async Task<Result<GetAdminSummaryDto>> GetAdminSummaryAsync()
    {
        if (string.IsNullOrWhiteSpace(adminId))
            return Result<GetAdminSummaryDto>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        var userSummary = await authContext.Users
            .AsNoTracking()
            .GroupBy(u => 1)
            .Select(g => new
            {
                TotalUsers = g.Count(),
                ActiveUsers = g.Where(u => u.IsActive).Count()
            })
            .FirstOrDefaultAsync();

        int totalTransactions = await breadContext.Transactions.CountAsync();

        int totalBudgets = await breadContext.Budgets.CountAsync();

        GetAdminSummaryDto summary = new GetAdminSummaryDto(
            userSummary!.TotalUsers,
            userSummary.ActiveUsers,
            totalTransactions,
            totalBudgets);

        return Result<GetAdminSummaryDto>.Success(summary);
    }

    public async Task<Result<IEnumerable<GetUsersAdminDto>>> GetUsersAdminAsync()
    {
        if (string.IsNullOrWhiteSpace(adminId))
            return Result<IEnumerable<GetUsersAdminDto>>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserIs is Required"));

        List<GetUsersAdminDto> userList = await authContext.Users
            .AsNoTracking()
            .Join(authContext.UserRoles,
                user => user.Id,
                userRole => userRole.UserId,
                (user, userRole) => new { user, userRole })
            .Join(authContext.Roles,
                combined => combined.userRole.RoleId,
                role => role.Id,
                (combined, role) => new GetUsersAdminDto(
                    combined.user.FullName,
                    combined.user.Email!,
                    role.Name!,
                    combined.user.IsActive ? "Active" : "NotActive",
                    combined.user.CreatedAtUtc))
            .ToListAsync();

        return Result<IEnumerable<GetUsersAdminDto>>.Success(userList);
    }

    public async Task<Result<GetUsersAdminDto>> GetUserAdminAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(adminId))
            return Result<GetUsersAdminDto>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        GetUsersAdminDto? user = await authContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Join(authContext.UserRoles,
                user => user.Id,
                userRole => userRole.UserId,
                (user, userRole) => new { user, userRole })
            .Join(authContext.Roles,
                combined => combined.userRole.RoleId,
                role => role.Id,
                (combined, role) => new GetUsersAdminDto(
                    combined.user.FullName,
                    combined.user.Email!,
                    role.Name!,
                    combined.user.IsActive ? "Active" : "NotActive",
                    combined.user.CreatedAtUtc))
            .FirstOrDefaultAsync();

        return user == null
            ? Result<GetUsersAdminDto>.NotFound(new Errors(ErrorCodes.NotFound, "User Not Found"))
            : Result<GetUsersAdminDto>.Success(user);
    }

    public async Task<Result<IEnumerable<GetAuditLogsDto>>> GetAuditLogsAdminAsync()
    {
        if (string.IsNullOrWhiteSpace(adminId))
            return Result<IEnumerable<GetAuditLogsDto>>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        List<AuditLog> auditLogs = await breadContext.AuditLogs
            .AsNoTracking()
            .ToListAsync();

        List<ApplicationUser> applicationUsers = await authContext.Users
            .AsNoTracking()
            .ToListAsync();

        List<GetAuditLogsDto> auditList = auditLogs.Join(applicationUsers,
            audit => audit.UserId,
            user => user.Id,
            (audit, user) => new GetAuditLogsDto(
                user.FullName,
                audit.Action,
                audit.Details,
                audit.CreatedAtUtc))
            .ToList();

        return Result<IEnumerable<GetAuditLogsDto>>.Success(auditList);
    }

    public async Task<Result<GetAdminDashBoardDto>> GetDashBoardAsync()
    {
        Result<GetAdminSummaryDto> summary = await GetAdminSummaryAsync();

        Result<IEnumerable<GetUsersAdminDto>> users = await GetUsersAdminAsync();

        Result<IEnumerable<GetAuditLogsDto>> audits = await GetAuditLogsAdminAsync();

        if (!summary.Succeeded)
            return Result<GetAdminDashBoardDto>.Failure(summary.Errors);

        if (!users.Succeeded)
            return Result<GetAdminDashBoardDto>.Failure(users.Errors);

        if (!audits.Succeeded)
            return Result<GetAdminDashBoardDto>.Failure(audits.Errors);

        GetAdminDashBoardDto getAdminDashBoardDto = new GetAdminDashBoardDto(
            summary.Value!,
            users.Value!.ToList(),
            audits.Value!.ToList());

        return Result<GetAdminDashBoardDto>.Success(getAdminDashBoardDto);
    }
}
