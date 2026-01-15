namespace SimpleOAuthTokenFetcher.PlatformProfiles;

public class DouyinPlatformProfile : IPlatformProfile
{
    public string Name { get; } = "Douyin";
    public string AuthorizeUrl { get; } = "https://open.douyin.com/platform/oauth/connect";
    public string TokenUrl { get; } = "https://open.douyin.com/oauth/access_token/";
    public List<string> Scopes { get; } = ["user_info", "trial.whitelist"];

    public string ClientIdParameterName => "client_key";

    public string ScopeDelimiter => ",";

    
}