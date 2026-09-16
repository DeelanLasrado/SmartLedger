using FluentValidation;
using MediatR;

namespace SmartLedger.Application.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<AuthTokenResult>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginHandler(IAuthService authService) : IRequestHandler<LoginCommand, AuthTokenResult>
{
    public Task<AuthTokenResult> Handle(LoginCommand request, CancellationToken ct) =>
        authService.LoginAsync(request.Email, request.Password, ct);
}

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthTokenResult>;

public class RefreshTokenHandler(IAuthService authService) : IRequestHandler<RefreshTokenCommand, AuthTokenResult>
{
    public Task<AuthTokenResult> Handle(RefreshTokenCommand request, CancellationToken ct) =>
        authService.RefreshAsync(request.RefreshToken, ct);
}
