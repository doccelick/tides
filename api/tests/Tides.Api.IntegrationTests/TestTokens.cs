using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Tides.Api.IntegrationTests;

public static class TestTokens
{
    public const string Issuer = "https://tides.test/issuer";

    public static SymmetricSecurityKey SigningKey { get; } = new(RandomNumberGenerator.GetBytes(32));

    public static string Create(string audience, params string[] roles)
    {
        var claims = new Dictionary<string, object>
        {
            ["oid"] = Guid.NewGuid().ToString(),
            ["scp"] = "access_as_user",
            ["ver"] = "2.0",
        };

        if (roles.Length > 0)
        {
            claims["roles"] = roles;
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            Claims = claims,
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
