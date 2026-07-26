using Bread.API.Application.Dtos.AuthDto;
using Bread.API.Common.Results;

namespace Bread.API.Application.Services
{
    public interface IUserServices
    {
        Task<Result<string>> LoginAsync(LoginUserDto loginUserDto);
        Task<Result<RegisteredUserDto>> RegisterUserAsync(RegisterUserDto registerUserDto);
    }
}