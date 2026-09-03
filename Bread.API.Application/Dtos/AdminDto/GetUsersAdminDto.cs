namespace Bread.API.Application.Dtos.AdminDto;

public record GetUsersAdminDto(
    string UserId,
    string FullName,
    string Email,
    string Role,
    string Status,
    DateTime CreatedAtUtc);
