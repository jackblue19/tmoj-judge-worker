using Application.UseCases.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Configurations.Auth;


public static class JwtAuthExtensions
{
    public static IServiceCollection AddTraditionalJwtAuth(this IServiceCollection services , IConfiguration config)
    {
        services.Configure<JwtOptions>(config.GetSection("Jwt"));
        services.AddSingleton<IConfigureOptions<JwtBearerOptions> , ConfigureJwtBearerOptions>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddSingleton<ITokenService , JwtTokenService>();
        services.AddSingleton<IRefreshTokenService , RefreshTokenService>();
        services.AddSingleton<IPasswordHasher , Pbkdf2PasswordHasher>();

        services.AddAuthorization();
        return services;
    }

    private sealed class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
    {
        private readonly JwtOptions _opt;

        public ConfigureJwtBearerOptions(IOptions<JwtOptions> opt)
            => _opt = opt.Value;

        public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName , options);

        public void Configure(string? name , JwtBearerOptions options)
        {
            var signingKey = BuildValidationKey(_opt);

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true ,
                ValidIssuer = _opt.Issuer ,

                ValidateAudience = true ,
                ValidAudience = _opt.Audience ,

                ValidateLifetime = true ,
                ClockSkew = TimeSpan.FromMinutes(1) ,

                ValidateIssuerSigningKey = true ,
                IssuerSigningKey = signingKey ,

                NameClaimType = "name" ,
                RoleClaimType = "role"
            };
        }


        private static SecurityKey BuildValidationKey(JwtOptions opt)
        {
            if ( !string.IsNullOrWhiteSpace(opt.Signing.PublicKeyPemPath) )
            {
                var pem = File.ReadAllText(opt.Signing.PublicKeyPemPath);
                var rsa = RSA.Create();
                rsa.ImportFromPem(pem);
                return new RsaSecurityKey(rsa) { KeyId = opt.Signing.KeyId };
            }

            if ( !string.IsNullOrWhiteSpace(opt.Signing.SymmetricKey) )
            {
                if ( opt.Signing.SymmetricKey.Length < 32 )
                    throw new InvalidOperationException("Jwt:Signing:SymmetricKey must be at least 32 characters.");
                return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opt.Signing.SymmetricKey));
            }

            throw new InvalidOperationException("JWT signing config missing. Provide PublicKeyPemPath or SymmetricKey.");
        }
    }
}