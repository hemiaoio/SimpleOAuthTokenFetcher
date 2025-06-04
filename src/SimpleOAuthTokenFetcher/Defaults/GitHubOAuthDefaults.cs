namespace SimpleOAuthTokenFetcher.Defaults;

public class GitHubOAuthDefaults
{
    public const string AuthorizeUrl = "https://github.com/login/oauth/authorize";
    public const string TokenUrl = "https://github.com/login/oauth/access_token";

    public static readonly string[] Scopes = new[]
    {
        "read:user",
        "repo",
        "user:email",
        "admin:org"
    };
}