using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SimpleOAuthTokenFetcher;
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
    new GitHubPlatformProfile(),
    new GooglePlatformProfile(),
    new WeChatPlatformProfile()
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
    var selectedScopes = AnsiConsole.Prompt(
        new MultiSelectionPrompt<string>()
            .Title("选择需要的 [green]Scopes[/]:")
            .AddChoices(platform.Scopes)
            .InstructionsText("[grey](↑/↓ 移动，空格选择，Enter确认)[/]")
    );

    var clientId = PromptRequired("请输入 [yellow]ClientId[/]:");
    var clientSecret = PromptRequired("请输入 [yellow]ClientSecret[/]:");
    var usePkce = AnsiConsole.Prompt(
        new SelectionPrompt<bool>()
            .Title("是否使用 PKCE？")
            .AddChoices(new[] { true, false })
            .UseConverter(b => b ? "是" : "否")
    );

    return new OAuthClientOptions
    {
        Name = platform.Name,
        ClientId = clientId,
        ClientSecret = clientSecret,
        RedirectUri = "http://localhost:8000/auth/callback",
        Scopes = selectedScopes,
        AuthorizeUrl = platform.AuthorizeUrl,
        TokenUrl = platform.TokenUrl,
        UsePkce = usePkce
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

    return new OAuthClientOptions
    {
        Name = name,
        ClientId = clientId,
        ClientSecret = clientSecret,
        RedirectUri = "http://localhost:8000/callback",
        Scopes = scopes,
        AuthorizeUrl = authUrl,
        TokenUrl = tokenUrl
    };
}

static string PromptRequired(string prompt)
{
    return AnsiConsole.Prompt(
        new TextPrompt<string>(prompt)
            .Validate(s => string.IsNullOrWhiteSpace(s)
                ? ValidationResult.Error("[red]此项不能为空[/]")
                : ValidationResult.Success())
    );
}