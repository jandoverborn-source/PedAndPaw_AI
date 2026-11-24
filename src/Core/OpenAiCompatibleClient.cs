using System.Net.Http;
using System.Text;

namespace PedAndPaw_AI.Core;

/// <summary>
/// Talks to any OpenAI-compatible chat completions endpoint: Gemini's compatible
/// endpoint, Ollama, OpenRouter, Groq or a private server.
///
/// The request body is built and the reply is read by hand, on purpose: a game mod
/// should not need System.Text.Json plus its transitive assemblies copied into the
/// player's scripts folder. The only dependency is System.Net.Http from the
/// framework. The same code runs on net48 and net8.0.
/// </summary>
public sealed class OpenAiCompatibleClient : IAiProvider, IDisposable
{
    private readonly HttpClient _http;
    private readonly ModConfig _config;
    private readonly string? _apiKey;

    /// <param name="apiKeyOverride">
    /// Key read from a local file. Used only when the environment variable is empty,
    /// because Proton does not always pass the launcher's environment to the game.
    /// </param>
    public OpenAiCompatibleClient(ModConfig config, string? apiKeyOverride = null)
    {
        _config = config;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(Math.Max(5, config.RequestTimeoutSeconds)) };

        var environmentKey = string.IsNullOrWhiteSpace(config.ApiKeyEnvVar)
            ? null
            : Environment.GetEnvironmentVariable(config.ApiKeyEnvVar);

        _apiKey = string.IsNullOrWhiteSpace(apiKeyOverride) ? environmentKey : apiKeyOverride;
        UsedLocalKeyFile = !string.IsNullOrWhiteSpace(apiKeyOverride) && string.IsNullOrWhiteSpace(environmentKey);
    }

    /// <summary>True when the key came from the local file rather than the environment.</summary>
    public bool UsedLocalKeyFile { get; }

    /// <summary>Why the last call failed, for the in-game log.</summary>
    public string? LastError { get; private set; }

    /// <summary>True when this provider can be called: a key is present, or none is needed.</summary>
    public bool HasCredentials =>
        string.Equals(_config.Provider, "ollama", StringComparison.OrdinalIgnoreCase) ||
        !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<string?> GetReplyAsync(AiRequest request, CancellationToken cancellationToken)
    {
        LastError = null;

        try
        {
            var url = _config.BaseUrl.TrimEnd('/') + "/chat/completions";
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);

            if (!string.IsNullOrWhiteSpace(_apiKey))
                httpRequest.Headers.Add("Authorization", "Bearer " + _apiKey);

            var body = "{" +
                       "\"model\":" + Json.Quote(_config.Model) + "," +
                       "\"messages\":[" +
                       "{\"role\":\"system\",\"content\":" + Json.Quote(request.SystemPrompt) + "}," +
                       "{\"role\":\"user\",\"content\":" + Json.Quote(request.UserMessage) + "}]," +
                       "\"max_tokens\":" + Math.Max(16, request.MaxTokens) + "," +
                       "\"temperature\":" + request.Temperature.ToString("0.0##", System.Globalization.CultureInfo.InvariantCulture) +
                       "}";

            httpRequest.Content = new StringContent(body, Encoding.UTF8, "application/json");

            using var response = await _http.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var detail = Json.TryReadString(responseBody, "message");
                LastError = "HTTP " + (int)response.StatusCode + (detail != null ? ": " + Json.Shorten(detail) : "");
                return null;
            }

            var text = Json.TryReadString(responseBody, "content");
            if (string.IsNullOrWhiteSpace(text))
            {
                LastError = "empty reply: " + Json.Shorten(responseBody);
                return null;
            }

            return text!.Trim();
        }
        catch (OperationCanceledException)
        {
            LastError = "timed out after " + _config.RequestTimeoutSeconds + "s";
            return null;
        }
        catch (Exception ex)
        {
            LastError = ex.GetType().Name + ": " + ex.Message;
            return null;
        }
    }

    public void Dispose() => _http.Dispose();
}

/// <summary>
/// Just enough JSON for one chat completions exchange: quote a string, read the
/// first value of a named property. No dependency, tolerant of arbitrary nesting.
/// </summary>
internal static class Json
{
    public static string Quote(string? value)
    {
        if (value == null) return "null";
        var sb = new StringBuilder(value.Length + 16);
        sb.Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                default:
                    if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }

    /// <summary>
    /// Find the first "name": "value" pair anywhere in the document and unescape it.
    /// Handles the shapes we actually receive, including an array root for errors.
    /// </summary>
    public static string? TryReadString(string? document, string propertyName)
    {
        if (document is null || document.Length == 0) return null;

        var needle = "\"" + propertyName + "\"";
        var at = document.IndexOf(needle, StringComparison.Ordinal);
        if (at < 0) return null;

        var i = at + needle.Length;
        while (i < document.Length && char.IsWhiteSpace(document[i])) i++;
        if (i >= document.Length || document[i] != ':') return null;
        i++;
        while (i < document.Length && char.IsWhiteSpace(document[i])) i++;
        if (i >= document.Length || document[i] != '"') return null;

        i++; // opening quote
        var sb = new StringBuilder();
        while (i < document.Length)
        {
            var c = document[i++];
            if (c == '"') return sb.ToString();
            if (c != '\\')
            {
                sb.Append(c);
                continue;
            }

            if (i >= document.Length) break;
            var esc = document[i++];
            switch (esc)
            {
                case 'n': sb.Append('\n'); break;
                case 'r': sb.Append('\r'); break;
                case 't': sb.Append('\t'); break;
                case 'b': sb.Append('\b'); break;
                case 'f': sb.Append('\f'); break;
                case 'u':
                    if (i + 4 <= document.Length)
                    {
                        sb.Append((char)Convert.ToInt32(document.Substring(i, 4), 16));
                        i += 4;
                    }
                    break;
                default: sb.Append(esc); break;
            }
        }

        return sb.ToString();
    }

    public static string Shorten(string? value)
    {
        if (value is null || value.Length == 0) return "";
        var single = value.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return single.Length <= 160 ? single : single.Substring(0, 160) + "...";
    }
}
