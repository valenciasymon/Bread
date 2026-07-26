using System.ComponentModel.DataAnnotations;

namespace Bread.API.Application.Dtos.AuthDto;

public class LoginUserDto
{
    [Required]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string Password { get; set; } = string.Empty;
}
