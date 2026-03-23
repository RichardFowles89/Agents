using Rag.Core.Models;

namespace Rag.Core.Contracts;

public interface IRagPipeline
{
    Task<AskResponse> AskAsync(AskRequest request, CancellationToken cancellationToken = default);
}
