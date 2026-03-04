using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Auth;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = default!;
    public string Audience { get; init; } = default!;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 14;

    public SigningOptions Signing { get; init; } = new();

    public sealed class SigningOptions
    {
        // RSA (recommended)
        public string? KeyId { get; init; }
        public string? PrivateKeyPemPath { get; init; }
        public string? PublicKeyPemPath { get; init; }

        // HS256 fallback
        public string? SymmetricKey { get; init; }
    }
}
