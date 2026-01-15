namespace SimpleOAuthTokenFetcher.Configuration;

public class OAuthClientOptions
{
    public string ClientIdParameterName { get; set; } = "client_id";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();

    public string ScopeParameterName { get; set; } = "scope";

    public string ScopeDelimiter { get; set; } = " ";
    public string TokenUrl { get; set; } = string.Empty;
    public string AuthorizeUrl { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = "http://localhost:8000/auth/callback";
    public bool UsePkce { get; set; } = false;
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this platform requires response_mode=form_post (e.g., Apple)
    /// </summary>
    public bool RequiresFormPost { get; set; } = false;

    /// <summary>
    /// Apple-specific options for dynamic client secret generation
    /// </summary>
    public AppleOAuthOptions? AppleOptions { get; set; }
}