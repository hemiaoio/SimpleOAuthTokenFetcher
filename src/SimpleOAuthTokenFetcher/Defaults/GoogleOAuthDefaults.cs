namespace SimpleOAuthTokenFetcher.Defaults;

public class GoogleOAuthDefaults
{
    public const string AuthorizeUrl = "https://accounts.google.com/o/oauth2/v2/auth";
    public const string TokenUrl = "https://oauth2.googleapis.com/token";

    public static readonly string[] Scopes = new[]
    {
        "openid",
        "profile",
        "email",
        "https://www.googleapis.com/auth/drive.readonly"
    };

}