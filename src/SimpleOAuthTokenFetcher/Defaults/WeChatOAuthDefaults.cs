namespace SimpleOAuthTokenFetcher.Defaults;

public class WeChatOAuthDefaults
{
    public const string AuthorizeUrl = "https://open.weixin.qq.com/connect/oauth2/authorize";
    public const string TokenUrl = "https://api.weixin.qq.com/sns/oauth2/access_token";

    public static readonly string[] Scopes = new[]
    {
        "snsapi_base",
        "snsapi_userinfo"
    };
}