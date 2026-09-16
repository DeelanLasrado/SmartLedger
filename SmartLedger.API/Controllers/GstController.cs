using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartLedger.API.Extensions;
using SmartLedger.Application.Gst.Commands;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class GstController(ISender sender) : ControllerBase
{
    public record ImportRequest(string Period, IReadOnlyList<GstEntryInput> Entries);
    public record ReconcileRequest(string Period);

    [HttpPost("import")]
    public async Task<ActionResult<object>> Import([FromBody] ImportRequest request, CancellationToken ct)
    {
        var count = await sender.Send(
            new ImportGstEntriesCommand(User.GetTenantId(), request.Period, request.Entries), ct);
        return Ok(new { imported = count });
    }

    [HttpPost("reconcile")]
    public async Task<ActionResult<GstReconciliationResult>> Reconcile(
        [FromBody] ReconcileRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new ReconcileGstCommand(User.GetTenantId(), request.Period), ct);
        return Ok(result);
    }
}
