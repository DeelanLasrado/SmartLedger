using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SmartLedger.Application.Auth.Commands;
using SmartLedger.Domain.Entities;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.Infrastructure.Identity;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    ITenantRepository tenantRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    JwtTokenService jwtTokenService,
    IOptions<JwtSettings> jwtOptions) : IAuthService
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    public async Task<RegisterTenantResult> RegisterAsync(RegisterTenantCommand command, CancellationToken ct = default)
    {
        var existing = await userManager.FindByEmailAsync(command.OwnerEmail);
        if (existing is not null)
            throw new InvalidOperationException("An account with this email already exists.");

        var tenant = Tenant.Create(command.BusinessName, command.OwnerEmail, command.Gstin);
        await tenantRepository.AddAsync(tenant, ct);

        var user = new ApplicationUser
        {
            UserName = command.OwnerEmail,
            Email = command.OwnerEmail,
            EmailConfirmed = true,
            TenantId = tenant.Id,
            FullName = command.BusinessName
        };

        var result = await userManager.CreateAsync(user, command.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        var accessToken = jwtTokenService.CreateAccessToken(user);
        var refreshToken = await IssueRefreshTokenAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new RegisterTenantResult(tenant.Id, user.Email!, accessToken, refreshToken);
    }

    public async Task<AuthTokenResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email)
            ?? throw new InvalidOperationException("Invalid email or password.");

        if (!await userManager.CheckPasswordAsync(user, password))
            throw new InvalidOperationException("Invalid email or password.");

        var accessToken = jwtTokenService.CreateAccessToken(user);
        var refreshToken = await IssueRefreshTokenAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new AuthTokenResult(accessToken, refreshToken, user.TenantId, user.Email!);
    }

    public async Task<AuthTokenResult> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var stored = await refreshTokenRepository.GetByTokenAsync(refreshToken, ct)
            ?? throw new InvalidOperationException("Invalid refresh token.");

        if (!stored.IsActive)
            throw new InvalidOperationException("Refresh token is expired or revoked.");

        var user = await userManager.FindByIdAsync(stored.UserId)
            ?? throw new InvalidOperationException("User not found.");

        stored.Revoke();
        await refreshTokenRepository.UpdateAsync(stored, ct);

        var accessToken = jwtTokenService.CreateAccessToken(user);
        var newRefresh = await IssueRefreshTokenAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new AuthTokenResult(accessToken, newRefresh, user.TenantId, user.Email!);
    }

    private async Task<string> IssueRefreshTokenAsync(ApplicationUser user, CancellationToken ct)
    {
        var token = JwtTokenService.CreateRefreshToken();
        var entity = RefreshToken.Create(
            user.TenantId,
            user.Id,
            token,
            DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays));

        await refreshTokenRepository.AddAsync(entity, ct);
        return token;
    }
}
