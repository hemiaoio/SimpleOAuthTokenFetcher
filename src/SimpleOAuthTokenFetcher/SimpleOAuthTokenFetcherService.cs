using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleOAuthTokenFetcher.Configuration;
using Spectre.Console;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace SimpleOAuthTokenFetcher
{
    public class SimpleOAuthTokenFetcherService : IHostedService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private ILogger<SimpleOAuthTokenFetcherService> Logger { get; }
        private OAuthClientOptions? _options;
        private string? _codeVerifier;
        private string? _lastRefreshToken;
        private string? _lastAccessToken;
        private string? _lastState;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleOAuthTokenFetcherService"/> class.
        /// </summary>
        /// <remarks>The <paramref name="httpClientFactory"/> parameter must not be null. Ensure that the
        /// factory is properly configured to create <see cref="System.Net.Http.HttpClient"/> instances suitable for the
        /// OAuth token fetching process.</remarks>
        /// <param name="httpClientFactory">A factory for creating <see cref="System.Net.Http.HttpClient"/> instances. This is used to send HTTP
        /// requests for fetching OAuth tokens.</param>
        /// <param name="logger"></param>
        public SimpleOAuthTokenFetcherService(IHttpClientFactory httpClientFactory,
            ILogger<SimpleOAuthTokenFetcherService> logger)
        {
            _httpClientFactory = httpClientFactory;
            Logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var code = await GetAuthorizationCodeAsync(_options);
            Logger.LogInformation("收到授权码：{code}", code);

            await ExchangeCodeForTokenAsync(code);
        }


        public async Task<string> GetAuthorizationCodeAsync(OAuthClientOptions options)
        {
            _lastState = Guid.NewGuid().ToString("N");
            var authUrl = $"{options.AuthorizeUrl}?response_type=code" +
                          $"&client_id={options.ClientId}" +
                          $"&redirect_uri={Uri.EscapeDataString(options.RedirectUri)}" +
                          $"&scope={Uri.EscapeDataString(string.Join(" ", options.Scopes))}" +
                          $"&state={_lastState}";
            if (options.UsePkce)
            {
                _codeVerifier = GenerateCodeVerifier();
                var codeChallenge = GenerateCodeChallenge(_codeVerifier);
                authUrl += $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
                           $"&code_challenge_method=S256";
                Logger.LogInformation("🔐 使用 PKCE 模式");
            }

            AnsiConsole.MarkupLine($"[green]请在浏览器中打开以下地址完成授权：[/]");
            AnsiConsole.WriteLine(authUrl);
            Process.Start(new ProcessStartInfo
            {
                FileName = authUrl,
                UseShellExecute = true
            });

            // 判断 redirectUri 是否为 localhost 模式
            if (IsLocalhostUri(options.RedirectUri))
            {
                return await WaitForCallbackAsync(options.RedirectUri);
            }

            return WaitForManualCode();
        }

        private async Task<string> WaitForCallbackAsync(string redirectUri)
        {
            var uri = new Uri(redirectUri);
            var listener = new HttpListener();
            listener.Prefixes.Add($"{uri.Scheme}://{uri.Host}:{uri.Port}/");
            listener.Start();

            AnsiConsole.MarkupLine("[blue]正在监听回调地址... 请完成授权。[/]");

            var context = await listener.GetContextAsync();
            var code = context.Request.QueryString["code"];

            // 简单反馈给浏览器
            var responseString = "<html><body>授权成功，可以关闭此页面。</body></html>";
            var buffer = Encoding.UTF8.GetBytes(responseString);
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            context.Response.Close();
            listener.Stop();

            return code;
        }

        private string WaitForManualCode()
        {
            AnsiConsole.MarkupLine("[yellow]由于 redirectUri 不是 localhost，请手动完成授权并粘贴浏览器跳转后的 URL：[/]");
            var input = AnsiConsole.Ask<string>("请输入完整的回调地址：");

            var uri = new Uri(input);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var code = query["code"];

            if (string.IsNullOrWhiteSpace(code))
            {
                throw new Exception("未能从回调地址中解析出 code 参数");
            }

            return code;
        }

        private bool IsLocalhostUri(string uri)
        {
            return uri.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase)
                   || uri.StartsWith("http://127.0.0.1", StringComparison.OrdinalIgnoreCase);
        }


        private async Task<string> WaitForCodeFromRedirectAsync()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8000/auth/callback/");
            listener.Start();
            Logger.LogInformation("⏳ 正在等待 OAuth 回调... （使用Ctrl+C退出等待并终止应用）");

            var context = await listener.GetContextAsync();
            var query = HttpUtility.ParseQueryString(context.Request.Url!.Query);
            var code = query.Get("code");
            var state = query.Get("state");
            if (_lastState != state)
            {
                Logger.LogError("❌ State 不匹配，可能是 CSRF 攻击或配置错误。请检查你的 OAuth 设置。");
            }

            var responseHtml = "<html charset=\"utf-8\"><body>授权成功，可关闭此窗口。</body></html>";
            var buffer = Encoding.UTF8.GetBytes(responseHtml);
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer);
            context.Response.OutputStream.Close();

            listener.Stop();
            return code ?? throw new Exception("未收到授权码");
        }

        private async Task<string> ExchangeCodeForTokenAsync(string code)
        {
            var client = _httpClientFactory.CreateClient();
            var values = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", _options.RedirectUri },
                { "client_id", _options.ClientId }
            };

            if (_options.UsePkce)
            {
                values["code_verifier"] = _codeVerifier!;
            }
            else
            {
                values["client_secret"] = _options.ClientSecret;
            }

            var content = new FormUrlEncodedContent(values);
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(_options.ClientId + ":" + _options.ClientSecret)));
            requestMessage.Content = content;
            var response = await client.SendAsync(requestMessage);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("❌ 获取 Token 失败：\n{body}", body);
                throw new Exception("Token 获取失败");
            }

            using var json = JsonDocument.Parse(body);
            var accessToken = json.RootElement.GetProperty("access_token").GetString();
            var refreshToken = json.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
            var expire = json.RootElement.GetProperty("expires_in").GetInt32();
            Logger.LogInformation("✅ Access Token：\n{accessToken}", accessToken);
            Logger.LogInformation("✅ Lifetime：\n{expire} Seconds", expire);
            if (refreshToken != null)
            {
                Logger.LogInformation("🔁 Refresh Token：\n{refreshToken}", refreshToken);
                // 保存 refresh_token 可用于续期
                _lastRefreshToken = refreshToken;
            }

            _lastAccessToken = accessToken;
            return accessToken!;
        }
        private static string GenerateCodeVerifier()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Base64UrlEncode(bytes);
        }

        private static string GenerateCodeChallenge(string codeVerifier)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            return Base64UrlEncode(bytes);
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public void SetOptions(OAuthClientOptions options)
        {
            _options = options;
        }

        public void SetRefreshToken(string token)
        {
            _lastRefreshToken = token;
        }
        public async Task RefreshAccessTokenAsync()
        {
            if (string.IsNullOrEmpty(_lastRefreshToken))
            {
                Logger.LogWarning("⚠️ 当前没有 refresh_token 可用，无法续期。");
                return;
            }

            Logger.LogInformation("🔄 正在使用 refresh_token 刷新 Access Token...");

            var client = _httpClientFactory.CreateClient();
            var values = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", _lastRefreshToken! },
                { "client_id", _options.ClientId }
            };

            if (!_options.UsePkce)
            {
                values["client_secret"] = _options.ClientSecret;
            }

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync(_options.TokenUrl, content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("❌ 刷新 Token 失败：\n{body}", body);
                return;
            }

            using var json = JsonDocument.Parse(body);
            var accessToken = json.RootElement.GetProperty("access_token").GetString();
            var refreshToken = json.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : _lastRefreshToken;

            _lastAccessToken = accessToken;
            _lastRefreshToken = refreshToken;

            Logger.LogInformation("✅ Access Token 已刷新：\n{accessToken}", accessToken);
            Logger.LogInformation("🔁 Refresh Token：\n{refreshToken}", refreshToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}