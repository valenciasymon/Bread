using Bread.API.Application.Dtos.AuthDto;
using Bread.API.Application.Services;
using Bread.API.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace Bread.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IUserServices userServices) : BaseApiController
{
    [HttpPost("register")]
    public async Task<ActionResult<RegisteredUserDto>> RegisterUser(
        [FromBody]RegisterUserDto registerUserDto)
    {
        Result<RegisteredUserDto> result = await userServices.RegisterUserAsync(registerUserDto);

        return ToActionResult<RegisteredUserDto>(result);
    }

    [HttpPost("breadadmin")]
    public async Task<ActionResult<RegisteredUserDto>> RegisterAdmin(
        [FromBody] RegisterUserDto registerUserDto)
    {
        Result<RegisteredUserDto> result = await userServices.RegisterUserAsync(registerUserDto);

        return ToActionResult<RegisteredUserDto>(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<string>> LoginUser(
        [FromBody]LoginUserDto loginUserDto)
    {
        Result<string> result = await userServices.LoginAsync(loginUserDto);

        return ToActionResult<string>(result);
    }
}
