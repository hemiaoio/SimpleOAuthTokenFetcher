namespace SimpleOAuthTokenFetcher.PlatformProfiles;

public class FacebookPlatformProfile : IPlatformProfile
{
    public string Name { get; } = "Facebook";
    public string AuthorizeUrl { get; } = "https://www.facebook.com/v23.0/dialog/oauth";
    public string TokenUrl { get; } = "https://graph.facebook.com/v23.0/oauth/access_token";
    public List<string> Scopes { get; } = new List<string>
    {
        "openid", // 访问用户 OpenID
        "email", // 访问用户电子邮件
        "public_profile", // 访问用户公开资料
    };
}