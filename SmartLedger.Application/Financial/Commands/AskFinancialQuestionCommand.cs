using FluentValidation;
using MediatR;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.Application.Financial.Commands;

public record AskFinancialQuestionCommand(Guid TenantId, string Question) : IRequest<string>;

public class AskFinancialQuestionValidator : AbstractValidator<AskFinancialQuestionCommand>
{
    public AskFinancialQuestionValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Question).NotEmpty().MaximumLength(1000);
    }
}

public class AskFinancialQuestionHandler(IFinancialQnAService qnaService)
    : IRequestHandler<AskFinancialQuestionCommand, string>
{
    public Task<string> Handle(AskFinancialQuestionCommand request, CancellationToken ct) =>
        qnaService.AskAsync(request.TenantId, request.Question, ct);
}
