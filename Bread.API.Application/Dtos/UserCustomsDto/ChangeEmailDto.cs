using System.ComponentModel.DataAnnotations;

namespace Bread.API.Application.Dtos.UserCustomsDto;

public class ChangeEmailDto 
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public required string Email { get; set; }
}
