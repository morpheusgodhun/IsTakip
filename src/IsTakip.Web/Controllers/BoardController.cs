using IsTakip.Application.WorkItems;
using IsTakip.Infrastructure.Persistence;
using IsTakip.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IsTakip.Web.Controllers;

[Authorize]
public class BoardController : Controller
{
    private readonly IWorkItemService _workItems;
    private readonly AppDbContext _db;

    public BoardController(IWorkItemService workItems, AppDbContext db)
    {
        _workItems = workItems;
        _db = db;
    }

    public async Task<IActionResult> Index(long? typeId)
    {
        var vm = new BoardViewModel
        {
            WorkItemTypeId = typeId,
            Columns = await _workItems.GetBoardAsync(typeId),
            Types = await _db.WorkItemTypes.Where(t => t.IsActive).OrderBy(t => t.Name)
                .Select(t => new SelectListItem(t.Name, t.Id.ToString())).ToListAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeState([FromForm] long workItemId, [FromForm] long toStateId)
    {
        var result = await _workItems.ChangeStateAsync(workItemId, toStateId);
        return Json(new { success = result.Succeeded, error = result.Error });
    }
}
