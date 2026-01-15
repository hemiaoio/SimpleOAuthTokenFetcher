namespace SimpleOAuthTokenFetcher.PlatformProfiles;

public interface IPlatformProfile
{
    string Name { get; }
    string AuthorizeUrl { get; }
    string TokenUrl { get; }
    List<string> Scopes { get; }
    virtual string ClientIdParameterName => "client_id";

    virtual string ScopeDelimiter => " ";

    /// <summary>
    /// Indicates if this platform requires dynamic client secret generation (e.g., Apple)
    /// </summary>
    virtual bool RequiresDynamicClientSecret => false;

    /// <summary>
    /// Indicates if this platform requires response_mode=form_post (e.g., Apple)
    /// </summary>
    virtual bool RequiresFormPost => false;
}