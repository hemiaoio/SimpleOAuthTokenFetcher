using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SimpleOAuthTokenFetcher;
using SimpleOAuthTokenFetcher.Apple;
using SimpleOAuthTokenFetcher.Configuration;
using SimpleOAuthTokenFetcher.PlatformProfiles;
using SimpleOAuthTokenFetcher.Serilog.Skin;
using Spectre.Console;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Sink(new SpectreConsoleSink())
    .Enrich.FromLogContext()
    .MinimumLevel.Debug()
    .CreateLogger();

Log.Logger.Information("OAuth 控制台启动中...");
var builder = Host.CreateDefaultBuilder(args)
    .UseSerilog() // 使用 Serilog 记录日志
    .ConfigureServices((_, services) =>
    {
        services.AddHttpClient();
        services.AddSingleton<SimpleOAuthTokenFetcherService>();
    });

using var host = builder.Build();
var options = LoadOAuthOptionsInteractively([
    new TwitterPlatformProfile(),
    new FacebookPlatformProfile(),
    new GitHubPlatformProfile(),
    new GooglePlatformProfile(),
    new ApplePlatformProfile(),
    new WeChatPlatformProfile(),
    new LinkedInPlatformProfile(),
    new BilibiliPlatformProfile(),
    new DouyinPlatformProfile()
]);
Log.Logger.Information("准备使用 {Name} 平台的 ClientId: {ClientId} 请求授权", options.Name, options.ClientId);
var service = host.Services.GetRequiredService<SimpleOAuthTokenFetcherService>();
service.SetOptions(options);

var choice = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("你想使用哪种方式获取 Access Token？")
        .AddChoices("1 - 新的授权流程", "2 - 使用 [yellow]refresh_token[/] 刷新")
);
if (choice.StartsWith("2"))
{
    var refreshToken = PromptRequired("请输入 [yellow]refresh_token[/]：");
    if (string.IsNullOrEmpty(refreshToken))
    {
        Log.Error("❌ refresh_token 不能为空，程序终止。");
        return;
    }

    service.SetRefreshToken(refreshToken);
    await service.RefreshAccessTokenAsync();
}
else
{
    await service.StartAsync(CancellationToken.None);
}

static OAuthClientOptions LoadOAuthOptionsInteractively(List<IPlatformProfile> profiles)
{

    // 添加“自定义平台”选项
    var allProfiles = new List<IPlatformProfile>(profiles)
    {
        new CustomPlatformProfile()
    };
    var profileMap = allProfiles.ToDictionary(p => p.Name, p => p);
    var platformName = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("请选择 [green]第三方平台[/]:")
            .AddChoices(profileMap.Keys.Select(item => item.EscapeMarkup()))
    );
    platformName = platformName.Replace("[[", "[").Replace("]]", "]"); // 恢复原始格式
    var platform = profileMap[platformName];
    if (platform is CustomPlatformProfile)
    {
        return PromptCustomPlatform();
    }

    // Handle Apple platform with dynamic client secret
    if (platform is ApplePlatformProfile applePlatform)
    {
        return PromptApplePlatform(applePlatform);
    }

    var selectedScopes = AnsiConsole.Prompt(
        new MultiSelectionPrompt<string>()
            .Title("选择需要的 [green]Scopes[/]:")
            .AddChoices(platform.Scopes)
            .InstructionsText("[grey](↑/↓ 移动，空格选择，Enter确认)[/]")
    );

    var clientId = PromptRequired("请输入 [yellow]ClientId[/]:");
    var clientSecret = PromptRequired("请输入 [yellow]ClientSecret[/]:");
    var redirectUri = PromptRequired("请输入 [yellow]RedirectUri[/]:", "https://localhost:8000/auth/callback");
    var usePkce = AnsiConsole.Prompt(
        new SelectionPrompt<bool>()
            .Title("是否使用 PKCE？")
            .AddChoices(new[] { true, false })
            .UseConverter(b => b ? "是" : "否")
    );

    return new OAuthClientOptions
    {
        ClientId = clientId,
        ClientSecret = clientSecret,
        RedirectUri = redirectUri,
        Scopes = selectedScopes,
        UsePkce = usePkce,
        Name = platform.Name,
        AuthorizeUrl = platform.AuthorizeUrl,
        TokenUrl = platform.TokenUrl,
        ClientIdParameterName = platform.ClientIdParameterName,
        ScopeDelimiter = platform.ScopeDelimiter,
    };
}

static OAuthClientOptions PromptCustomPlatform()
{
    AnsiConsole.MarkupLine("[yellow]您选择了自定义平台，请输入以下信息：[/]");

    var name = PromptRequired("平台名称:");
    var authUrl = PromptRequired("授权地址 (Auth URL):");
    var tokenUrl = PromptRequired("令牌地址 (Token URL):");
    var scopeInput = PromptRequired("请输入 Scope（使用空格分隔多个）:");
    var scopes = scopeInput.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

    var clientId = PromptRequired("请输入 [yellow]ClientId[/]:");
    var clientSecret = PromptRequired("请输入 [yellow]ClientSecret[/]:");
    var redirectUri = PromptRequired("请输入 [yellow]RedirectUri[/]:", "https://localhost:8000/auth/callback");

    return new OAuthClientOptions
    {
        Name = name,
        ClientId = clientId,
        ClientSecret = clientSecret,
        RedirectUri = redirectUri,
        Scopes = scopes,
        AuthorizeUrl = authUrl,
        TokenUrl = tokenUrl
    };
}

static OAuthClientOptions PromptApplePlatform(ApplePlatformProfile platform)
{
    AnsiConsole.MarkupLine("[yellow]Apple 登录需要使用 p8 私钥动态生成 ClientSecret[/]");

    var selectedScopes = AnsiConsole.Prompt(
        new MultiSelectionPrompt<string>()
            .Title("选择需要的 [green]Scopes[/]:")
            .AddChoices(platform.Scopes)
            .InstructionsText("[grey](↑/↓ 移动，空格选择，Enter确认)[/]")
    );

    var teamId = PromptRequired("请输入 [yellow]Team ID[/] (10位字符):");
    var keyId = PromptRequired("请输入 [yellow]Key ID[/]:");
    var clientId = PromptRequired("请输入 [yellow]Service ID (Client ID)[/]:");
    var privateKeyPath = PromptRequired("请输入 [yellow]p8 私钥文件路径[/]:");
    var redirectUri = PromptRequired("请输入 [yellow]RedirectUri[/]:", "https://localhost:8000/auth/callback");

    // Validate p8 file exists
    if (!File.Exists(privateKeyPath))
    {
        Log.Error("❌ 私钥文件不存在: {path}", privateKeyPath);
        throw new FileNotFoundException("私钥文件不存在", privateKeyPath);
    }

    var appleOptions = new AppleOAuthOptions
    {
        TeamId = teamId,
        KeyId = keyId,
        ClientId = clientId,
        PrivateKeyPath = privateKeyPath,
        ExpirationMinutes = 5
    };

    // Generate client secret
    var clientSecret = AppleClientSecretGenerator.GenerateClientSecret(appleOptions);
    Log.Information("✅ 已动态生成 Apple ClientSecret (JWT)");

    return new OAuthClientOptions
    {
        ClientId = clientId,
        ClientSecret = clientSecret,
        RedirectUri = redirectUri,
        Scopes = selectedScopes,
        UsePkce = false,
        Name = platform.Name,
        AuthorizeUrl = platform.AuthorizeUrl,
        TokenUrl = platform.TokenUrl,
        ClientIdParameterName = platform.ClientIdParameterName,
        ScopeDelimiter = platform.ScopeDelimiter,
        RequiresFormPost = platform.RequiresFormPost,
        AppleOptions = appleOptions
    };
}

static string PromptRequired(string prompt, string? defaultValue = null)
{
    return AnsiConsole.Prompt(
        new TextPrompt<string>(prompt)
            .DefaultValue(defaultValue)
            .ShowDefaultValue(true)
    );
}