using IsTakip.Application.WorkItems;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsTakip.Web.Api;

[ApiController]
[Route("api/work-items")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class WorkItemsApiController : ControllerBase
{
    private readonly IWorkItemService _workItems;

    public WorkItemsApiController(IWorkItemService workItems) => _workItems = workItems;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] WorkItemFilter filter)
        => Ok(await _workItems.GetListAsync(filter));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        var item = await _workItems.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkItemRequest request)
    {
        var result = await _workItems.CreateAsync(request);
        return result.Succeeded
            ? CreatedAtAction(nameof(Get), new { id = result.Data }, new { id = result.Data })
            : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:long}/state")]
    public async Task<IActionResult> ChangeState(long id, [FromBody] long toStateId)
    {
        var result = await _workItems.ChangeStateAsync(id, toStateId);
        return result.Succeeded ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpGet("board")]
    public async Task<IActionResult> Board([FromQuery] long? typeId)
        => Ok(await _workItems.GetBoardAsync(typeId));
}
