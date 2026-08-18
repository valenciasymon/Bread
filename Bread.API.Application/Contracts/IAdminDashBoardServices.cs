using Bread.API.Application.Dtos.AdminDto;
using Bread.API.Common.Results;

namespace Bread.API.Application.Contracts
{
    public interface IAdminDashBoardServices
    {
        Task<Result<IEnumerable<GetAuditLogsDto>>> GetAuditLogsAdminAsync();
        Task<Result<GetAdminDashBoardDto>> GetDashBoardAsync();
        Task<Result<GetUsersAdminDto>> GetUserAdminAsync(string userId);
        Task<Result<IEnumerable<GetUsersAdminDto>>> GetUsersAdminAsync();
    }
}