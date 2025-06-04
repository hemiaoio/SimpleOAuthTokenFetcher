using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using SimpleOAuthTokenFetcher;
using SimpleOAuthTokenFetcher.Configuration;
using SimpleOAuthTokenFetcher.Defaults;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddHttpClient();
        services.AddSingleton<SimpleOAuthTokenFetcherService>();
    });

using var host = builder.Build();
var options = LoadOAuthOptionsInteractively();
var service = host.Services.GetRequiredService<SimpleOAuthTokenFetcherService>();
service.SetOptions(options);

Console.WriteLine("\n你想使用哪种方式获取 Access Token？");
Console.WriteLine("1 - 新的授权流程");
Console.WriteLine("2 - 使用 refresh_token 刷新");
Console.Write("请输入选项（1/2）：");

var choice = Console.ReadLine()?.Trim();

if (choice == "2")
{
    Console.Write("\n请输入 refresh_token：");
    var refreshToken = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(refreshToken))
    {
        Console.WriteLine("❌ refresh_token 不能为空，程序终止。");
        return;
    }

    service.SetRefreshToken(refreshToken);
    await service.RefreshAccessTokenAsync();
}
else
{
    await service.StartAsync(CancellationToken.None);
}

static string[] SelectScopesInteractively(string[] scopes)
{
    var selected = new bool[scopes.Length];
    int current = 0;

    ConsoleKey key;
    do
    {
        Console.Clear();
        Console.WriteLine("🔹 使用 ↑ ↓ 选择 Scope，空格切换选中，Enter 完成：\n");

        for (int i = 0; i < scopes.Length; i++)
        {
            var prefix = (i == current ? "👉 " : "   ") + (selected[i] ? "[✔]" : "[ ]");
            Console.WriteLine($"{prefix} {scopes[i]}");
        }

        var keyInfo = Console.ReadKey(true);
        key = keyInfo.Key;

        switch (key)
        {
            case ConsoleKey.UpArrow:
                current = (current - 1 + scopes.Length) % scopes.Length;
                break;
            case ConsoleKey.DownArrow:
                current = (current + 1) % scopes.Length;
                break;
            case ConsoleKey.Spacebar:
                selected[current] = !selected[current];
                break;
        }

    } while (key != ConsoleKey.Enter);

    var selectedScopes = scopes
        .Where((_, index) => selected[index])
        .ToArray();

    if (selectedScopes.Length == 0)
    {
        Console.WriteLine("\n⚠️ 你没有选择任何 Scope，将使用默认第一个：");
        selectedScopes = new[] { scopes[0] };
    }

    Console.Clear();
    Console.WriteLine("✅ 你选择了以下 Scope：\n");
    foreach (var s in selectedScopes)
    {
        Console.WriteLine($"- {s}");
    }

    Console.WriteLine();

    return selectedScopes;
}

static OAuthClientOptions LoadFromPreset(string platform, string authorizeUrl, string tokenUrl, string[] scopes)
{
    var options = new OAuthClientOptions
    {
        AuthorizeUrl = authorizeUrl,
        TokenUrl = tokenUrl
    };

    Console.WriteLine($"\n✅ 你选择了平台：{platform}");

    // 输入 clientId 和 clientSecret
    options.ClientId = PromptRequired("Client ID");
    options.ClientSecret = PromptRequired("Client Secret");

    var selectedScopes = SelectScopesInteractively(scopes);
    options.Scope = string.Join(' ', selectedScopes);

    Console.Write("是否启用 PKCE？(y/N): ");
    var usePkce = Console.ReadLine()?.Trim().ToLower();
    options.UsePkce = (usePkce == "y" || usePkce == "yes");

    options.RedirectUri = "http://localhost:8000/auth/callback";

    Console.WriteLine($"使用默认 Redirect URI: {options.RedirectUri}");
    return options;
}
static OAuthClientOptions LoadManually()
{
    Console.WriteLine("\n🔧 自定义平台，请手动输入配置信息：");
    var options = new OAuthClientOptions
    {
        ClientId = PromptRequired("Client ID"),
        ClientSecret = PromptRequired("Client Secret"),
        AuthorizeUrl = PromptRequired("Authorize URL"),
        TokenUrl = PromptRequired("Token URL")
    };

    Console.Write("Scope（可选，默认 read）：");
    var scope = Console.ReadLine()?.Trim();
    if (!string.IsNullOrEmpty(scope))
        options.Scope = scope;
    else
        options.Scope = "read";

    options.RedirectUri = "http://localhost:8000/callback";
    Console.WriteLine($"使用默认 Redirect URI: {options.RedirectUri}");
    return options;
}

static OAuthClientOptions LoadOAuthOptionsInteractively()
{
    Console.WriteLine("请选择 OAuth 平台：");
    Console.WriteLine("1. Google");
    Console.WriteLine("2. GitHub");
    Console.WriteLine("3. WeChat");
    Console.WriteLine("4. X");
    Console.WriteLine("0. 自定义平台");

    Console.Write("请输入编号：");
    var input = Console.ReadLine()?.Trim();

    OAuthClientOptions options;

    switch (input)
    {
        case "1":
            options = LoadFromPreset("Google", GoogleOAuthDefaults.AuthorizeUrl, GoogleOAuthDefaults.TokenUrl, GoogleOAuthDefaults.Scopes);
            break;
        case "2":
            options = LoadFromPreset("GitHub", GitHubOAuthDefaults.AuthorizeUrl, GitHubOAuthDefaults.TokenUrl, GitHubOAuthDefaults.Scopes);
            break;
        case "3":
            options = LoadFromPreset("WeChat", WeChatOAuthDefaults.AuthorizeUrl, WeChatOAuthDefaults.TokenUrl, WeChatOAuthDefaults.Scopes);
            break;
        case "4":
            options = LoadFromPreset("X", TwitterOAuthDefaults.AuthorizeUrl, TwitterOAuthDefaults.TokenUrl, TwitterOAuthDefaults.Scopes);
            break;
        default:
            options = LoadManually();
            break;
    }

    return options;
}

static string PromptRequired(string label)
{
    string? input;
    do
    {
        Console.Write($"{label}: ");
        input = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input))
        {
            Console.WriteLine($"❗ {label} 不能为空，请重新输入。");
        }
    } while (string.IsNullOrEmpty(input));

    return input;
}