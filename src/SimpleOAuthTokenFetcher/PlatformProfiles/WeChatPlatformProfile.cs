namespace SimpleOAuthTokenFetcher.PlatformProfiles;

public class WeChatPlatformProfile : IPlatformProfile
{
    public string Name => "WeChat";
    public string AuthorizeUrl => "https://open.weixin.qq.com/connect/oauth2/authorize";
    public string TokenUrl => "https://api.weixin.qq.com/sns/oauth2/access_token";

    public List<string> Scopes { get; } =
    [
        "snsapi_base",
        "snsapi_userinfo"
    ];
}