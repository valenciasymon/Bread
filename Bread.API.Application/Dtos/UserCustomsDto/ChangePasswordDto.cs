using System.ComponentModel.DataAnnotations;

namespace Bread.API.Application.Dtos.UserCustomsDto;

public class ChangePasswordDto 
{
    [Required]
    public required string CurrentPassword { get; set; }

    [Required]
    [MinLength(8)]
    public required string NewPassword { get; set; }
}
