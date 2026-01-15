namespace SimpleOAuthTokenFetcher.PlatformProfiles;

public class GooglePlatformProfile : IPlatformProfile
{
    public string Name => "Google";
    public string AuthorizeUrl => "https://accounts.google.com/o/oauth2/v2/auth";
    public string TokenUrl => "https://oauth2.googleapis.com/token";

    public List<string> Scopes { get; } =
    [
        "openid",
        "https://www.googleapis.com/auth/userinfo.profile",
        "https://www.googleapis.com/auth/userinfo.email"
    ];
}