namespace SimpleOAuthTokenFetcher.PlatformProfiles;

public class GitHubPlatformProfile : IPlatformProfile
{
    public string Name => "GitHub";
    public string AuthorizeUrl => "https://github.com/login/oauth/authorize";
    public string TokenUrl => "https://github.com/login/oauth/access_token";

    public List<string> Scopes { get; } =
    [
        "repo", // 访问仓库
        "user", // 访问用户信息
        "gist", // 访问 Gist
        "notifications", // 访问通知
        "read:org", // 读取组织信息
        "workflow"
    ];
}