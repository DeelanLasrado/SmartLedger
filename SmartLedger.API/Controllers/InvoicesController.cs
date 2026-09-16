using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartLedger.API.Extensions;
using SmartLedger.Application.Invoices.Commands;
using SmartLedger.Application.Invoices.DTOs;
using SmartLedger.Application.Invoices.Queries;

namespace SmartLedger.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class InvoicesController(ISender sender) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<InvoiceParseResult>> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { title = "File is required." });

        await using var stream = file.OpenReadStream();
        var result = await sender.Send(
            new ParseInvoiceCommand(User.GetTenantId(), stream, file.FileName), ct);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceDto>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetInvoicesQuery(User.GetTenantId(), page, pageSize), ct);
        return Ok(result);
    }
}
