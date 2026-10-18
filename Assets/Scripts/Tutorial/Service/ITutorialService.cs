#nullable enable

using System.Threading;
using Cysharp.Threading.Tasks;
using Tutorial.State;

namespace Tutorial.Service
{
    public interface ITutorialService
    {
        UniTask ShowIfFirstTimeAsync(TutorialId id, CancellationToken cancellationToken);
    }
}
