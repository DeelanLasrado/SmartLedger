using FluentValidation;
using MediatR;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.Application.CashFlow.Queries;

public record GetCashFlowForecastQuery(Guid TenantId, int HorizonDays = 30)
    : IRequest<CashFlowForecastResult>;

public class GetCashFlowForecastValidator : AbstractValidator<GetCashFlowForecastQuery>
{
    public GetCashFlowForecastValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.HorizonDays).Must(d => d is 30 or 60 or 90)
            .WithMessage("Horizon must be 30, 60, or 90 days.");
    }
}

public class GetCashFlowForecastHandler(ICashFlowForecaster forecaster, ICacheService cache)
    : IRequestHandler<GetCashFlowForecastQuery, CashFlowForecastResult>
{
    public async Task<CashFlowForecastResult> Handle(GetCashFlowForecastQuery request, CancellationToken ct)
    {
        var cacheKey = $"cashflow:{request.TenantId}:{request.HorizonDays}";
        var cached = await cache.GetAsync<CashFlowForecastResult>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var result = await forecaster.ForecastAsync(request.TenantId, request.HorizonDays, ct);
        await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30), ct);
        return result;
    }
}
