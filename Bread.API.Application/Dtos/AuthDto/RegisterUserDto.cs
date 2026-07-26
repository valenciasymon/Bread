using System.ComponentModel.DataAnnotations;

namespace Bread.API.Application.Dtos.AuthDto;

public class RegisterUserDto
{
    [Required]
    [Length(1,20)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Length(1, 20)]
    public string LastName { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Length(4, 30)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}
