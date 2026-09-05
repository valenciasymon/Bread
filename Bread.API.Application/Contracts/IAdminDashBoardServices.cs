using Bread.API.Application.Dtos.AdminDto;
using Bread.API.Application.Dtos.AuthDto;
using Bread.API.Common.Models.Filtering;
using Bread.API.Common.Models.Paging;
using Bread.API.Common.Results;
using Microsoft.AspNetCore.JsonPatch;

namespace Bread.API.Application.Contracts
{
    public interface IAdminDashBoardServices
    {
        Task<Result<PagedResult<GetAuditLogsDto>>> GetAuditLogsAdminAsync(
            PaginationParameters paginationParameters,
            AuditsFilterParameters filterParameters);
        Task<Result<GetAdminSummaryDto>> GetAdminSummaryAsync();
        Task<Result<PagedResult<GetUsersAdminDto>>> GetUsersAdminAsync(
            PaginationParameters paginationParameters,
            UsersFilterParameters filterParameters);
        Task<Result> PatchStatusAdmin(string userId, JsonPatchDocument<UpdateUserDto> patch);
    }
}