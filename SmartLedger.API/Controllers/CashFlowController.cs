using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartLedger.API.Extensions;
using SmartLedger.Application.CashFlow.Queries;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CashFlowController(ISender sender) : ControllerBase
{
    [HttpGet("forecast")]
    public async Task<ActionResult<CashFlowForecastResult>> Forecast(
        [FromQuery] int horizonDays = 30, CancellationToken ct = default)
    {
        var result = await sender.Send(
            new GetCashFlowForecastQuery(User.GetTenantId(), horizonDays), ct);
        return Ok(result);
    }
}
