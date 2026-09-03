using Bread.API.Application.Dtos.AdminDto;
using Bread.API.Application.Dtos.AuthDto;
using Bread.API.Common.Models.Paging;
using Bread.API.Common.Results;
using Microsoft.AspNetCore.JsonPatch;

namespace Bread.API.Application.Contracts
{
    public interface IAdminDashBoardServices
    {
        Task<Result<PagedResult<GetAuditLogsDto>>> GetAuditLogsAdminAsync(
            PaginationParameters paginationParameters);
        Task<Result<GetAdminSummaryDto>> GetAdminSummaryAsync();
        Task<Result<PagedResult<GetUsersAdminDto>>> GetUsersAdminAsync(
            PaginationParameters paginationParameters);
        Task<Result> PatchStatusAdmin(string userId, JsonPatchDocument<UpdateUserDto> patch);
    }
}