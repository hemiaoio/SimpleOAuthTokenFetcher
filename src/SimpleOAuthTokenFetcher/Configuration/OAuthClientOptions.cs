namespace SimpleOAuthTokenFetcher.Configuration;

public class OAuthClientOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string TokenUrl { get; set; } = string.Empty;
    public string AuthorizeUrl { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = "http://localhost:8000/auth/callback";
    public bool UsePkce { get; set; } = false;
}