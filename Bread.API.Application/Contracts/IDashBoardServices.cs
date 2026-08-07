using Bread.API.Application.Dtos.DashBoardDto;
using Bread.API.Common.Results;

namespace Bread.API.Application.Contracts
{
    public interface IDashBoardServices
    {
        Task<Result<GetDashBoardDto>> GetDashBoardAsync();
    }
}