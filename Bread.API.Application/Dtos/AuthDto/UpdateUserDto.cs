namespace Bread.API.Application.Dtos.AuthDto;

public record UpdateUserDto(
    string UserId,
    bool IsActive);
