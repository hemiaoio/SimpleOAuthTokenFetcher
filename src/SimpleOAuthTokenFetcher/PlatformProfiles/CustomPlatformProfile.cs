namespace SimpleOAuthTokenFetcher.PlatformProfiles;

public class CustomPlatformProfile : IPlatformProfile
{
    public string Name => "[自定义平台]";
    public string AuthorizeUrl => "Custom";
    public string TokenUrl => "Custom";

    public List<string> Scopes { get; } = ["read"];
}