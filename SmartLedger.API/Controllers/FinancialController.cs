using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartLedger.API.Extensions;
using SmartLedger.Application.Financial.Commands;

namespace SmartLedger.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class FinancialController(ISender sender) : ControllerBase
{
    public record AskRequest(string Question);

    [HttpPost("ask")]
    public async Task<ActionResult<object>> Ask([FromBody] AskRequest request, CancellationToken ct)
    {
        var answer = await sender.Send(
            new AskFinancialQuestionCommand(User.GetTenantId(), request.Question), ct);
        return Ok(new { answer });
    }
}
