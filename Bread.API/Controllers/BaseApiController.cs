using Bread.API.AuthorizationFilters;
using Bread.API.Common.Constants;
using Bread.API.Common.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bread.API.Controllers;

public abstract class BaseApiController : ControllerBase
{
    public ActionResult ToActionResult<T>(Result<T> result)
    {
        return result.Succeeded
            ? Ok(result.Value)
            : MapErrorToActionResult(result.Errors);
    }

    public ActionResult ToActionResult(Result result)
    {
        return result.Succeeded
            ? NoContent()
            : MapErrorToActionResult(result.Errors);
    }

    public ActionResult MapErrorToActionResult(Errors[] errors)
    {
        if (errors.Length == 0) return Problem();

        Errors e = errors[0];

        return e.Code switch
        {
            ErrorCodes.NotFound => NotFound(e.Description),
            ErrorCodes.Conflict => Conflict(e.Description),
            ErrorCodes.Validation => BadRequest(e.Description),
            ErrorCodes.BadRequest => BadRequest(e.Description),
            ErrorCodes.Forbidden => Forbid(e.Description),
            ErrorCodes.Unauthorized => Unauthorized(e.Description),
            _ => Problem(detail: e.Description, title: e.Code)
        };
    }
}
