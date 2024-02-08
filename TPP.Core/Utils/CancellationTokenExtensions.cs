using System.Threading;
using System.Threading.Tasks;

namespace TPP.Core.Utils;

public static class CancellationTokenExtensions
{
    // A task for when the cancellation token is cancelled,
    // as per https://github.com/dotnet/runtime/issues/14991#issuecomment-131221355
    public static Task WhenCanceled(this CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();
        cancellationToken.Register(s => ((TaskCompletionSource<bool>)s!).SetResult(true), tcs);
        return tcs.Task;
    }
}
