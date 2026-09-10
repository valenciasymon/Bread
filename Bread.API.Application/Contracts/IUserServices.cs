using Bread.API.Application.Dtos.AuthDto;
using Bread.API.Application.Dtos.UserCustomsDto;
using Bread.API.Common.Results;

namespace Bread.API.Application.Services
{
    public interface IUserServices
    {
        Task<Result<string>> LoginAsync(LoginUserDto loginUserDto);
        Task<Result<RegisteredUserDto>> RegisterUserAsync(RegisterUserDto registerUserDto);
        Task<Result<RegisteredUserDto>> RegisterAdminAsync(RegisterUserDto registerUserDto);
        Task<Result> UpdateNameAsync(ChangeNameDto changeNameDto);
        Task<Result> UpdateEmailAsync(ChangeEmailDto changeEmailDto);
        Task<Result> UpdatePasswordAsync(ChangePasswordDto changePasswordDto);
    }
}