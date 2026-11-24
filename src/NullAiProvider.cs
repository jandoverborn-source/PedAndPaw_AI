using System.Threading;
using System.Threading.Tasks;
using PedAndPaw_AI.Core;

namespace PedAndPaw_AI
{
    /// <summary>
    /// Placeholder provider used until the HTTP client exists (step 4).
    /// It answers deterministically, so the whole interaction loop can be played
    /// and tested in game without a network call or an API key.
    /// </summary>
    public sealed class NullAiProvider : IAiProvider
    {
        public Task<string> GetReplyAsync(AiRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(
                "[no AI provider yet] I can hear you. Connect a provider in PedAndPaw_AI.ini for a real answer.");
        }
    }
}
