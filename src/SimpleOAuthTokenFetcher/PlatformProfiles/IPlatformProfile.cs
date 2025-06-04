namespace SimpleOAuthTokenFetcher.PlatformProfiles;

public interface IPlatformProfile
{
    string Name { get; }
    string AuthorizeUrl { get; }
    string TokenUrl { get; }
    List<string> Scopes { get; }
}