using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Assistant;

namespace SchoolManagementSystem.Web.Controllers;

[Authorize]
public class AssistantController : Controller
{
    private readonly AiAssistantService _assistantService;

    public AssistantController(AiAssistantService assistantService)
    {
        _assistantService = assistantService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask([FromBody] ChatRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            return Json(new ChatResponse
            {
                Success = false,
                Reply = "Введите сообщение."
            });
        }

        if (request.Message.Length > 1000)
        {
            return Json(new ChatResponse
            {
                Success = false,
                Reply = "Сообщение слишком длинное."
            });
        }

        var (reply, navigateUrl) = await _assistantService.AskAsync(
            User,
            request.Message.Trim(),
            request.History ?? new List<ChatHistoryItem>());

        return Json(new ChatResponse
        {
            Success = true,
            Reply = reply,
            NavigateUrl = navigateUrl
        });
    }
}
