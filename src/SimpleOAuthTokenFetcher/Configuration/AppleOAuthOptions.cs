namespace SimpleOAuthTokenFetcher.Configuration;

public class AppleOAuthOptions
{
    /// <summary>
    /// Apple Team ID (10 character string)
    /// </summary>
    public string TeamId { get; set; } = string.Empty;

    /// <summary>
    /// Key ID from Apple Developer Portal
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Client ID (Service ID or Bundle ID)
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Path to the .p8 private key file
    /// </summary>
    public string PrivateKeyPath { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in minutes (max 6 months = 15777000 seconds)
    /// </summary>
    public int ExpirationMinutes { get; set; } = 5;
}
