namespace SimpleOAuthTokenFetcher.PlatformProfiles;

public class TwitterPlatformProfile : IPlatformProfile
{
    public string Name => "X";
    public string AuthorizeUrl => "https://x.com/i/oauth2/authorize";
    public string TokenUrl => "https://api.x.com/2/oauth2/token";

    public List<string> Scopes { get; } =
    [
        "tweet.read",
        "tweet.write",
        "tweet.moderate.write",
        "users.email",
        "users.read",
        "follows.read",
        "follows.write",
        "offline.access",
        "space.read",
        "mute.read",
        "mute.write",
        "like.read",
        "like.write",
        "list.read",
        "list.write",
        "block.read",
        "block.write",
        "bookmark.read",
        "bookmark.write",
        "media.write"
    ];
}