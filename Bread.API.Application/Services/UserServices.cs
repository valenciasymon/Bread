
using Bread.API.Application.Dtos.AuthDto;
using Bread.API.Common.Constants;
using Bread.API.Common.Models.Config;
using Bread.API.Common.Results;
using Bread.API.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Bread.API.Application.Services;

public class UserServices(
    UserManager<ApplicationUser> userManager,
    IOptions<JwtSettings> options,
    AuthDbContext context) : IUserServices
{
    public async Task<Result<RegisteredUserDto>> RegisterUserAsync(RegisterUserDto registerUserDto)
    {
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            ApplicationUser user = new ApplicationUser
            {
                Email = registerUserDto.Email,
                FirstName = registerUserDto.FirstName,
                LastName = registerUserDto.LastName,
                UserName = registerUserDto.UserName
            };

            IdentityResult userResult = await userManager
                .CreateAsync(user, registerUserDto.Password);

            if (!userResult.Succeeded)
            {
                await transaction.RollbackAsync();

                return Result<RegisteredUserDto>.BadRequest(userResult.Errors.Select(e => new Errors(
                    ErrorCodes.BadRequest, e.Description)).ToArray());
            }

            IdentityResult roleResult = await userManager.AddToRoleAsync(user, RolesConstants.User);

            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();

                return Result<RegisteredUserDto>.BadRequest(userResult.Errors.Select(e => new Errors(
                    ErrorCodes.BadRequest, e.Description)).ToArray());
            }

            await transaction.CommitAsync();

            RegisteredUserDto registeredUserDto = new RegisteredUserDto
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Role = RolesConstants.User
            };

            return Result<RegisteredUserDto>.Success(registeredUserDto);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();

            return Result<RegisteredUserDto>.Failure(new Errors(
                ErrorCodes.Failure, "Unexpected Server Error"));
        }

    }

    public async Task<Result<RegisteredUserDto>> RegisterAdminAsync(RegisterUserDto registerUserDto)
    {
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            ApplicationUser user = new ApplicationUser
            {
                FirstName = registerUserDto.FirstName,
                LastName = registerUserDto.LastName,
                Email = registerUserDto.Email,
                UserName = registerUserDto.UserName
            };

            IdentityResult userResult = await userManager.CreateAsync(user, registerUserDto.Password);

            if (!userResult.Succeeded)
            {
                await transaction.RollbackAsync();

                return Result<RegisteredUserDto>.BadRequest(userResult.Errors.Select(
                    e => new Errors(ErrorCodes.BadRequest, e.Description)).ToArray());
            }

            IdentityResult roleResult = await userManager.AddToRoleAsync(user, RolesConstants.Admin);

            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();

                return Result<RegisteredUserDto>.BadRequest(roleResult.Errors.Select(
                    e => new Errors(ErrorCodes.BadRequest, e.Description)).ToArray());
            }

            await transaction.CommitAsync();

            RegisteredUserDto registeredUserDto = new RegisteredUserDto
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Role = RolesConstants.Admin
            };

            return Result<RegisteredUserDto>.Success(registeredUserDto);
        }
        catch(Exception)
        {
            await transaction.RollbackAsync();

            return Result<RegisteredUserDto>.Failure(new Errors(
                ErrorCodes.Failure, "Unexpected Server Error"));
        }
    }

    public async Task<Result<string>> LoginAsync(LoginUserDto loginUserDto)
    {
        ApplicationUser? user = await userManager.FindByEmailAsync(loginUserDto.Email);

        if (user == null) return Result<string>.Unauthorized(new Errors(
            ErrorCodes.Unauthorized, "Invalid Email or Password"));

        bool isPasswordValid = await userManager.CheckPasswordAsync(user, loginUserDto.Password);

        if (!isPasswordValid) return Result<string>.Unauthorized(new Errors(
            ErrorCodes.Unauthorized, "Invalid Email or Password"));

        string token = await GenerateToken(user);

        return Result<string>.Success(token);
    }

    private async Task<string> GenerateToken(ApplicationUser user)
    {
        List<Claim> claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Name, user.FullName),
            new Claim("username",user.UserName!),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        IList<string> roles = await userManager.GetRolesAsync(user);
        List<Claim> rolesClaim = roles.Select(r => new Claim(ClaimTypes.Role, r)).ToList();

        claims.AddRange(rolesClaim);

        SymmetricSecurityKey securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(options.Value.Key));

        SigningCredentials signingCreds = new SigningCredentials(
            securityKey, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new JwtSecurityToken(
            issuer: options.Value.Issuer,
            audience: options.Value.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(options.Value.DurationInMinutes),
            signingCredentials: signingCreds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
