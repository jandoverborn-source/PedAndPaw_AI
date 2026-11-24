namespace PedAndPaw_AI.Core;

/// <summary>
/// Anything that can turn an <see cref="AiRequest"/> into a line of dialogue.
/// Implementations must be async and must never block the game thread.
/// </summary>
public interface IAiProvider
{
    /// <summary>
    /// Returns the reply text, or null when no reply could be produced.
    /// Throwing is allowed; the caller turns it into a log line, never a crash.
    /// </summary>
    Task<string?> GetReplyAsync(AiRequest request, CancellationToken cancellationToken);
}
