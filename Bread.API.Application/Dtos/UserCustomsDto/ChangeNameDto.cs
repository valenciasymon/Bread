using System.ComponentModel.DataAnnotations;

namespace Bread.API.Application.Dtos.UserCustomsDto;

public class ChangeNameDto 
{
    [Required]
    [Length(1,20)]
    public required string FirstName { get; set; }

    [Required]
    [Length(1,20)]
    public required string LastName { get; set; }
}
