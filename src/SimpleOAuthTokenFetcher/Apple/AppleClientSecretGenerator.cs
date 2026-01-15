using Microsoft.IdentityModel.Tokens;
using SimpleOAuthTokenFetcher.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace SimpleOAuthTokenFetcher.Apple;

public static class AppleClientSecretGenerator
{
    /// <summary>
    /// Generates a client secret JWT for Apple Sign In
    /// </summary>
    /// <param name="options">Apple OAuth options containing TeamId, KeyId, ClientId and PrivateKeyPath</param>
    /// <returns>A signed JWT string to use as client_secret</returns>
    public static string GenerateClientSecret(AppleOAuthOptions options)
    {
        var privateKey = LoadPrivateKey(options.PrivateKeyPath);
        var securityKey = new ECDsaSecurityKey(privateKey) { KeyId = options.KeyId };
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.EcdsaSha256);

        var now = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim("sub", options.ClientId),
            new Claim("iat", new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        var token = new JwtSecurityToken(
            issuer: options.TeamId,
            audience: "https://appleid.apple.com",
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(options.ExpirationMinutes),
            signingCredentials: credentials
        );

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(token);
    }

    private static ECDsa LoadPrivateKey(string privateKeyPath)
    {
        var privateKeyContent = File.ReadAllText(privateKeyPath);

        // Remove PEM headers and whitespace
        privateKeyContent = privateKeyContent
            .Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("-----END PRIVATE KEY-----", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim();

        var privateKeyBytes = Convert.FromBase64String(privateKeyContent);

        var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);

        return ecdsa;
    }
}
