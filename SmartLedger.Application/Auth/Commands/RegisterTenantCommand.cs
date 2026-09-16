using FluentValidation;
using MediatR;

namespace SmartLedger.Application.Auth.Commands;

public record RegisterTenantCommand(
    string BusinessName,
    string OwnerEmail,
    string Password,
    string? Gstin) : IRequest<RegisterTenantResult>;

public record RegisterTenantResult(Guid TenantId, string Email, string AccessToken, string RefreshToken);

public class RegisterTenantValidator : AbstractValidator<RegisterTenantCommand>
{
    public RegisterTenantValidator()
    {
        RuleFor(x => x.BusinessName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OwnerEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public interface IAuthService
{
    Task<RegisterTenantResult> RegisterAsync(RegisterTenantCommand command, CancellationToken ct = default);
    Task<AuthTokenResult> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<AuthTokenResult> RefreshAsync(string refreshToken, CancellationToken ct = default);
}

public record AuthTokenResult(string AccessToken, string RefreshToken, Guid TenantId, string Email);

public class RegisterTenantHandler(IAuthService authService)
    : IRequestHandler<RegisterTenantCommand, RegisterTenantResult>
{
    public Task<RegisterTenantResult> Handle(RegisterTenantCommand request, CancellationToken ct) =>
        authService.RegisterAsync(request, ct);
}
