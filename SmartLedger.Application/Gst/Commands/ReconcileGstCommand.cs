using FluentValidation;
using MediatR;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.Application.Gst.Commands;

public record ReconcileGstCommand(Guid TenantId, string Period) : IRequest<GstReconciliationResult>;

public class ReconcileGstCommandValidator : AbstractValidator<ReconcileGstCommand>
{
    public ReconcileGstCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Period)
            .Matches(@"^\d{4}-(0[1-9]|1[0-2])$")
            .WithMessage("Period must be YYYY-MM.");
    }
}

public class ReconcileGstHandler(IGstReconciliationService reconciliationService, ICacheService cache)
    : IRequestHandler<ReconcileGstCommand, GstReconciliationResult>
{
    public async Task<GstReconciliationResult> Handle(ReconcileGstCommand request, CancellationToken ct)
    {
        var cacheKey = $"gst:recon:{request.TenantId}:{request.Period}";
        var cached = await cache.GetAsync<GstReconciliationResult>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var result = await reconciliationService.ReconcileAsync(request.TenantId, request.Period, ct);
        await cache.SetAsync(cacheKey, result, TimeSpan.FromHours(1), ct);
        return result;
    }
}
