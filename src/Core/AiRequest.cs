namespace PedAndPaw_AI.Core;

/// <summary>
/// One chat-completion request, provider agnostic.
/// </summary>
public class AiRequest
{
    /// <summary>System prompt built by <see cref="PromptBuilder"/>.</summary>
    public string SystemPrompt { get; set; } = "";

    /// <summary>User message built by <see cref="PromptBuilder"/>.</summary>
    public string UserMessage { get; set; } = "";

    /// <summary>Hard cap on generated tokens, from Limits.MaxTokensPerReply.</summary>
    public int MaxTokens { get; set; } = 120;

    /// <summary>Sampling temperature, from Ai.Temperature.</summary>
    public float Temperature { get; set; } = 0.8f;

    /// <summary>Stable key for the NPC this request belongs to (rate limiting).</summary>
    public string NpcKey { get; set; } = "";
}
