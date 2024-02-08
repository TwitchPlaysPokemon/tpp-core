using System;
using System.Threading;
using System.Threading.Tasks;

namespace TPP.Core;

/// <summary>
/// This interface is just a semantic bundle for the typical use-case of having many, concurrently running features.
/// All such features can be started and then normally keep running until application shutdown is requested.
/// The recommended usage is to start many lifecycle features, collect all their tasks,
/// and then await all of them concurrently using <see cref="Task.WhenAll(System.Collections.Generic.IEnumerable{System.Threading.Tasks.Task})"/>,
/// so that any one task failing becomes visible immediately (in contrast to not awaiting the task until shutdown,
/// even though it could already be in an failure state that only bubbles up when awaiting).
/// </summary>
public interface IWithLifecycle
{
    /// <summary>
    /// Start running this feature.
    /// This typically starts some sort of long-running task that gets cancelled on shutdown.
    /// The task should only fail for critical failures that would justify an application shutdown,
    /// and otherwise handle any errors internally, e.g. by just logging.
    /// It should also not throw a <see cref="OperationCanceledException"/> when being cancelled.
    /// </summary>
    /// <param name="cancellationToken">The token to stop this feature, e.g. on shutdown.</param>
    /// <returns>A task that typically keeps running until cancelled.
    /// Awaiting the task afterwards is recommended for a graceful shutdown.</returns>
    Task Start(CancellationToken cancellationToken);
}
