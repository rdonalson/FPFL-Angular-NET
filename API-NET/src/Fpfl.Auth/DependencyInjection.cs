using Fpfl.Auth.Application.Security;
using Fpfl.Auth.Application.Services;
using Fpfl.Auth.Domain.Repositories;
using Fpfl.Auth.Infrastructure.Persistence;
using Fpfl.Auth.Infrastructure.Persistence.Repositories;
using Fpfl.Auth.Infrastructure.Security;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Fpfl.Auth;

/// <summary>
///     Registers the Auth bounded context and JWT bearer authentication.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddFpflAuth(
        this IServiceCollection services, IConfiguration configuration, string connectionString)
    {
        var section = configuration.GetSection(AuthOptions.SectionName);
        services.Configure<AuthOptions>(section);

        var authOptions = section.Get<AuthOptions>() ?? new AuthOptions();
        var signingKey = authOptions.CreateSigningKey(); // fails fast at startup if the key is missing

        services.TryAddSingleton(TimeProvider.System);

        services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserRolesRepository, UserRolesRepository>();
        services.AddScoped<IUserRolesService, UserRolesService>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = authOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = AuthClaims.Subject,
                    RoleClaimType = AuthClaims.Role
                };
            });

        services.AddAuthorization();

        return services;
    }
}
