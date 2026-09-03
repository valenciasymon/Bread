using Bread.API.Application.Contracts;
using Bread.API.Application.Dtos.AdminDto;
using Bread.API.Application.Dtos.AuthDto;
using Bread.API.Application.Dtos.DashBoardDto;
using Bread.API.AuthorizationFilters;
using Bread.API.Common.Constants;
using Bread.API.Common.Models.Paging;
using Bread.API.Common.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Bread.API.Controllers;


[ApiController]
[Route("api/[controller]")]
[RequireActiveUser]
[Authorize(Roles = RolesConstants.Admin)]
public class AdminDashBoardController(IAdminDashBoardServices adminServices) : BaseApiController
{
    [HttpGet("summary")]
    public async Task<ActionResult<GetAdminDashBoardDto>> GetAdminSummary()
    {
        Result<GetAdminSummaryDto> result = await adminServices.GetAdminSummaryAsync();

        return ToActionResult(result);
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<GetUsersAdminDto>>> GetUsers(
        [FromQuery]PaginationParameters paginationParameters)
    {
        Result<PagedResult<GetUsersAdminDto>> result = await adminServices.GetUsersAdminAsync(
            paginationParameters);

        return ToActionResult(result);
    }

    [HttpGet("audits")]
    public async Task<ActionResult<PagedResult<GetAuditLogsDto>>> GetAudits(
        [FromQuery] PaginationParameters paginationParameters)
    {
        Result<PagedResult<GetAuditLogsDto>> result = await adminServices.GetAuditLogsAdminAsync(
            paginationParameters);

        return ToActionResult(result);
    }

    [HttpPatch("{userId}")]
    public async Task<ActionResult> PatchStatus(
        string userId,
        [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
    {
        if (patchDoc == null)
            return BadRequest("PatchDoc is Required");

        if (patchDoc.Operations.Count == 0)
            return BadRequest("PatchDoc Cannot Be Empty");

        Result result = await adminServices.PatchStatusAdmin(userId,patchDoc);

        return ToActionResult(result);
    }
}
