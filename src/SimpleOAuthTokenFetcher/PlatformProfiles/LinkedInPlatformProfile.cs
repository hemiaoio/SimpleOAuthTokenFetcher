namespace SimpleOAuthTokenFetcher.PlatformProfiles
{
    internal class LinkedInPlatformProfile : IPlatformProfile
    {
        public string Name => "LinkedIn";

        public string AuthorizeUrl => "https://www.linkedin.com/oauth/v2/authorization";

        public string TokenUrl => "https://www.linkedin.com/oauth/v2/accessToken";

        public List<string> Scopes => "r_liteprofile r_emailaddress w_member_social".Split(' ').ToList();
    }
}
