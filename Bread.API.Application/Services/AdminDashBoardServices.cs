using Azure;
using Bread.API.Application.Contracts;
using Bread.API.Application.Dtos.AdminDto;
using Bread.API.Application.Dtos.AuthDto;
using Bread.API.Common.Constants;
using Bread.API.Common.Models.Extensions;
using Bread.API.Common.Models.Paging;
using Bread.API.Common.Results;
using Bread.API.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Transactions;

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

    public async Task<Result<GetAdminSummaryDto>> GetAdminSummaryAsync()
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

    public async Task<Result<PagedResult<GetUsersAdminDto>>> GetUsersAdminAsync(
        PaginationParameters paginationParameters)
    {
        if (string.IsNullOrWhiteSpace(adminId))
            return Result<PagedResult<GetUsersAdminDto>>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserIs is Required"));

        PagedResult<GetUsersAdminDto> userList = await authContext.Roles
            .AsNoTracking()
            .Where(r => r.Name == RolesConstants.User)
            .Join(authContext.UserRoles,
                role => role.Id,
                userRole => userRole.RoleId,
                (role, userRole) => new { role, userRole })
            .Join(authContext.Users,
                combined => combined.userRole.UserId,
                user => user.Id,
                (combined, user) => new GetUsersAdminDto(
                    user.Id,
                    user.FullName,
                    user.Email!,
                    combined.role.Name!,
                    user.IsActive ? "Active" : "NotActive",
                    user.CreatedAtUtc))
            .ToPagedResult<GetUsersAdminDto>(paginationParameters);

        return Result<PagedResult<GetUsersAdminDto>>.Success(userList);
    }


    public async Task<Result> PatchStatusAdmin(
        string userId,
        JsonPatchDocument<UpdateUserDto> patchDoc)
    {
        if (string.IsNullOrWhiteSpace(adminId))
            return Result.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        ApplicationUser? user = await authContext.Users
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync();

        if (user == null)
            return Result.NotFound(new Errors(
                ErrorCodes.NotFound, "User Not Found"));

        foreach (var operation in patchDoc.Operations)
        {
            if (operation.path != "/isActive" || operation.OperationType != OperationType.Replace)
                return Result.BadRequest(new Errors(
                    ErrorCodes.BadRequest, "Wrong PatchDoc Format"));
        }

        try
        {
            //retry strategy
            var strategy = breadContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                //db temp saving point
                using TransactionScope scope = new TransactionScope(
                TransactionScopeAsyncFlowOption.Enabled);

                UpdateUserDto updateUserDto = new UpdateUserDto(
                    user.Id,
                    user.IsActive);

                patchDoc.ApplyTo(updateUserDto);

                if (user.Id != updateUserDto.UserId)
                    return Result.BadRequest(new Errors(
                        ErrorCodes.BadRequest, "Cannot Change UserId"));

                user.IsActive = updateUserDto.IsActive;

                AuditLog auditLog = new AuditLog
                {
                    UserId = adminId,
                    Action = user.IsActive ? AuditActions.Activated : AuditActions.Deactivated,
                    Details = $"{AuditDetails.User}{userId}"
                };

                await breadContext.AuditLogs.AddAsync(auditLog);
                await breadContext.SaveChangesAsync();
                await authContext.SaveChangesAsync();

                scope.Complete();

                return Result.Success();
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            if(! await authContext.Users.AnyAsync(u => u.Id == userId))
            {
                return Result.NotFound(new Errors(
                    ErrorCodes.NotFound,"User Not Found"));
            }
            else
            {
                throw;
            }
        }
        catch (Exception)
        {
            return Result.Failure(new Errors(
                ErrorCodes.Failure,"Unexpected Server Error"));
        }
    }


    public async Task<Result<PagedResult<GetAuditLogsDto>>> GetAuditLogsAdminAsync(
        PaginationParameters paginationParameters)
    {
        if (string.IsNullOrWhiteSpace(adminId))
            return Result<PagedResult<GetAuditLogsDto>>.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        PagedResult<AuditLog> auditLogs = await breadContext.AuditLogs
            .AsNoTracking()
            .ToPagedResult<AuditLog>(paginationParameters);

        var users = await authContext.Users
            .AsNoTracking()
            .Join(authContext.UserRoles,
                user => user.Id,
                userRoles => userRoles.UserId,
                (user, userRoles) => new { user, userRoles })
            .Join(authContext.Roles,
                combined => combined.userRoles.RoleId,
                role => role.Id,
                (combined, role) => new
                {
                    UserId = combined.user.Id,
                    FullName = combined.user.FullName,
                    role
                })
            .ToListAsync();

        List<GetAuditLogsDto> mergeList = auditLogs.Data
            .Join(users,
                audits => audits.UserId,
                user => user.UserId,
                (audits, user) => new GetAuditLogsDto(
                    audits.AuditLogId,
                    user.FullName,
                    user.role.Name!,
                    audits.Action,
                    audits.Details,
                    audits.CreatedAtUtc))
            .ToList();

        PagedResult<GetAuditLogsDto> auditList = new PagedResult<GetAuditLogsDto>
        {
            Data = mergeList,
            MetaData = auditLogs.MetaData
        };

        return Result<PagedResult<GetAuditLogsDto>>.Success(auditList);
    }
}
