using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Application.Common.AI;

public static class AiHash
{
    public static string Create(object value)
    {
        var json = JsonSerializer.Serialize(value , new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string Truncate(string? value , int maxChars)
    {
        if ( string.IsNullOrEmpty(value) )
            return string.Empty;

        if ( maxChars <= 0 )
            return string.Empty;

        return value.Length <= maxChars ? value : value[..maxChars];
    }
}