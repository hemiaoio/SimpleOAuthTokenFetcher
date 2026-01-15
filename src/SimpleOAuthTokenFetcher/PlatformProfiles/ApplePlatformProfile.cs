namespace SimpleOAuthTokenFetcher.PlatformProfiles;

public class ApplePlatformProfile : IPlatformProfile
{
    public string Name => "Apple";
    public string AuthorizeUrl => "https://appleid.apple.com/auth/authorize";
    public string TokenUrl => "https://appleid.apple.com/auth/token";
    public string ClientIdParameterName => "client_id";
    public string ScopeDelimiter => " ";

    public List<string> Scopes { get; } =
    [
        "name",
        "email"
    ];

    /// <summary>
    /// Apple requires response_mode=form_post for web sign-in
    /// </summary>
    public bool RequiresFormPost => true;

    /// <summary>
    /// Indicates that Apple requires dynamic client secret generation
    /// </summary>
    public bool RequiresDynamicClientSecret => true;
}
