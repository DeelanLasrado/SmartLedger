using System.Text;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SmartLedger.Application.Auth.Commands;
using SmartLedger.Domain.Interfaces;
using SmartLedger.Infrastructure.BackgroundJobs;
using SmartLedger.Infrastructure.Caching;
using SmartLedger.Infrastructure.Common;
using SmartLedger.Infrastructure.Gst;
using SmartLedger.Infrastructure.Identity;
using SmartLedger.Infrastructure.Messaging;
using SmartLedger.Infrastructure.Persistence;
using SmartLedger.Infrastructure.Persistence.Repositories;
using SmartLedger.Infrastructure.Storage;
using StackExchange.Redis;

namespace SmartLedger.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddMemoryCache();

        var useInMemory = configuration.GetValue("UseInMemoryDatabase", true);
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (useInMemory || string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("SmartLedgerDemo"));
        }
        else
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
        }

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        var jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                  ?? new JwtSettings();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();

        if (useInMemory || string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseMemoryStorage());
        }
        else
        {
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(connectionString));
        }

        services.AddHangfireServer();

        var redisCs = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisCs))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisCs));
        }

        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IGstEntryRepository, GstEntryRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICacheService, MemoryCacheService>();
        services.AddScoped<IAlertService, InMemoryAlertService>();
        services.AddScoped<IGstReconciliationService, GstReconciliationService>();
        services.AddScoped<GstReportGenerationJob>();

        var blobCs = configuration["Azure:BlobConnectionString"];
        if (!string.IsNullOrWhiteSpace(blobCs))
            services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
        else
            services.AddScoped<IBlobStorageService, LocalBlobStorageService>();

        return services;
    }
}
