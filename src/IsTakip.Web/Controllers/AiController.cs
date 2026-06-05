using IsTakip.Application.Common;
using IsTakip.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsTakip.Web.Controllers;

[Authorize]
public class AiController : Controller
{
    private readonly IAiAssistant _ai;
    public AiController(IAiAssistant ai) => _ai = ai;

    [HttpGet]
    public IActionResult Index() => View(new AiChatVM { IsConfigured = _ai.IsConfigured });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string prompt)
    {
        var vm = new AiChatVM { Prompt = prompt, IsConfigured = _ai.IsConfigured };
        if (!string.IsNullOrWhiteSpace(prompt))
            vm.Answer = await _ai.AskAsync(prompt);
        return View(vm);
    }
}
