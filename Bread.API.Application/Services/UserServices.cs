
using Bread.API.Application.Dtos.AuthDto;
using Bread.API.Application.Dtos.UserCustomsDto;
using Bread.API.Common.Constants;
using Bread.API.Common.Models.Config;
using Bread.API.Common.Results;
using Bread.API.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Text;
using System.Transactions;

namespace Bread.API.Application.Services;

public class UserServices(
    UserManager<ApplicationUser> userManager,
    IOptions<JwtSettings> options,
    IHttpContextAccessor httpContext,
    BreadDbContext breadDbContext,
    AuthDbContext context) : IUserServices
{
    private string userId = httpContext.HttpContext.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? httpContext.HttpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? string.Empty;


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

                return Result<RegisteredUserDto>.BadRequest(roleResult.Errors.Select(e => new Errors(
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


    public async Task<Result> UpdateNameAsync(ChangeNameDto changeNameDto)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result.Forbidden(new Errors(ErrorCodes.Forbidden,"UserId is Required"));

        ApplicationUser? user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return Result.NotFound(new Errors(ErrorCodes.NotFound, "User Not Found"));

        try
        {
            IExecutionStrategy strategy = breadDbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using TransactionScope scope = new TransactionScope(
                    TransactionScopeAsyncFlowOption.Enabled);

                user.FirstName = changeNameDto.FirstName;
                user.LastName = changeNameDto.LastName;

                AuditLog auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = AuditActions.Updated,
                    Details = $"{AuditDetails.User}Updated Name"
                };

                await breadDbContext.AddAsync(auditLog);

                await context.SaveChangesAsync();
                await breadDbContext.SaveChangesAsync();

                scope.Complete();

                return Result.Success();
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            if (! await UserExist())
            {
                return Result.NotFound(new Errors(ErrorCodes.NotFound, "User Not Found"));
            }
            else
            {
                throw;
            }
        }
        catch (Exception)
        {
            return Result.Failure(new Errors(ErrorCodes.Failure, "Unexpected Server Error"));
        }
    }

    public async Task<Result> UpdateEmailAsync(ChangeEmailDto changeEmailDto)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result.Forbidden(new Errors(ErrorCodes.Forbidden, "UserId is Required"));

        ApplicationUser? user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return Result.NotFound(new Errors(ErrorCodes.NotFound, "User Not Found"));

        try
        {
            IExecutionStrategy strategy = breadDbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using TransactionScope scope = new TransactionScope(
                    TransactionScopeAsyncFlowOption.Enabled);

                IdentityResult emailResult = await userManager.SetEmailAsync(
                    user,
                    changeEmailDto.Email);

                if (!emailResult.Succeeded)
                    return Result.BadRequest(emailResult.Errors.Select(e => new Errors(
                        ErrorCodes.BadRequest,e.Description)).ToArray());

                AuditLog auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = AuditActions.Updated,
                    Details = $"{AuditDetails.User}Updated Email"
                };

                await userManager.UpdateSecurityStampAsync(user);

                await breadDbContext.AddAsync(auditLog);

                await context.SaveChangesAsync();
                await breadDbContext.SaveChangesAsync();

                scope.Complete();

                return Result.Success();
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await UserExist())
            {
                return Result.NotFound(new Errors(ErrorCodes.NotFound, "User Not Found"));
            }
            else
            {
                throw;
            }
        }
        catch (Exception)
        {
            return Result.Failure(new Errors(ErrorCodes.Failure, "Unexpected Server Error"));
        }
    }

    public async Task<Result> UpdatePasswordAsync(ChangePasswordDto changePasswordDto)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result.Forbidden(new Errors(
                ErrorCodes.Forbidden, "UserId is Required"));

        ApplicationUser? user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return Result.NotFound(new Errors(
                ErrorCodes.NotFound, "User Not Found"));

        try
        {
            IExecutionStrategy strategy = breadDbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using TransactionScope scope = new TransactionScope(
                    TransactionScopeAsyncFlowOption.Enabled);

                IdentityResult passResult = await userManager.ChangePasswordAsync(
                    user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);

                if (!passResult.Succeeded)
                    return Result.BadRequest(passResult.Errors.Select(e => new Errors(
                        ErrorCodes.BadRequest, e.Description)).ToArray());

                AuditLog auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = AuditActions.Updated,
                    Details = $"{AuditDetails.User}Updated Password"
                };

                await breadDbContext.AddAsync(auditLog);
                await breadDbContext.SaveChangesAsync();
                await context.SaveChangesAsync();

                scope.Complete();

                return Result.Success();
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            if(! await UserExist())
            {
                return Result.NotFound(new Errors(ErrorCodes.NotFound, "User Not Found"));
            }
            else
            {
                throw;
            }
        }
        catch (Exception)
        {
            return Result.Failure(new Errors(ErrorCodes.Failure, "Unexpected Server Error"));
        }
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
            new Claim(JwtDefaults.SecurityStamp,user.SecurityStamp!),
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

    private async Task<bool> UserExist()
    {
        return await context.Users.AnyAsync(u => u.Id == userId);
    }
}
