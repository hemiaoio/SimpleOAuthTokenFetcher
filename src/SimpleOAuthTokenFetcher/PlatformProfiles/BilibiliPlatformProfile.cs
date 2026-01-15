namespace SimpleOAuthTokenFetcher.PlatformProfiles;

public class BilibiliPlatformProfile:IPlatformProfile
{
    public string Name { get; } = "Bilibili";
    public string AuthorizeUrl { get; } = "https://account.bilibili.com/pc/account-pc/auth/oauth";
    public string TokenUrl { get; } = "https://api.bilibili.com/x/account-oauth2/v1/token";
    public List<string> Scopes { get; } = ["USER_INFO"];
}