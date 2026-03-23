using Application.UseCases.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Configurations.Auth;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _opt;
    private readonly SigningCredentials _creds;

    public JwtTokenService(IOptions<JwtOptions> opt)
    {
        _opt = opt.Value;
        _creds = new SigningCredentials(BuildSigningKey(_opt) , GetAlg(_opt));
        if ( !string.IsNullOrWhiteSpace(_opt.Signing.KeyId) && _creds.Key is not null )
            _creds.Key.KeyId = _opt.Signing.KeyId;
    }

    public string CreateAccessToken(string userId ,
                                    string? userName ,
                                    IEnumerable<string> roles ,
                                    IDictionary<string , string>? extraClaims = null)
    {
        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(now).ToString(), ClaimValueTypes.Integer64)
        };

        if ( !string.IsNullOrWhiteSpace(userName) )
            claims.Add(new Claim("name" , userName));

        foreach ( var r in roles.Distinct(StringComparer.OrdinalIgnoreCase) )
            claims.Add(new Claim("role" , r));

        if ( extraClaims is not null )
        {
            foreach ( var kv in extraClaims )
                claims.Add(new Claim(kv.Key , kv.Value));
        }

        claims.Add(new Claim(ClaimTypes.NameIdentifier , userId));      // refactor v2

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer ,
            audience: _opt.Audience ,
            claims: claims ,
            notBefore: now ,
            expires: now.AddMinutes(_opt.AccessTokenMinutes) ,
            signingCredentials: _creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GetAlg(JwtOptions opt)
        => !string.IsNullOrWhiteSpace(opt.Signing.PrivateKeyPemPath)
            ? SecurityAlgorithms.RsaSha256
            : SecurityAlgorithms.HmacSha256;

    private static SecurityKey BuildSigningKey(JwtOptions opt)
    {
        // RSA signing (optional)
        if ( !string.IsNullOrWhiteSpace(opt.Signing.PrivateKeyPemPath) )
        {
            var pem = File.ReadAllText(opt.Signing.PrivateKeyPemPath);
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);

            return new RsaSecurityKey(rsa)
            {
                KeyId = opt.Signing.KeyId
            };
        }

        // HS256 default
        if ( !string.IsNullOrWhiteSpace(opt.Signing.SymmetricKey) )
        {
            if ( opt.Signing.SymmetricKey.Length < 32 )
                throw new InvalidOperationException("Jwt:Signing:SymmetricKey must be at least 32 characters.");
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opt.Signing.SymmetricKey));
        }

        throw new InvalidOperationException("JWT signing config missing. Provide PrivateKeyPemPath or SymmetricKey.");
    }
}
