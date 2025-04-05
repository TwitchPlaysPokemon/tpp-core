using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TPP.Core;

public static class EventUtils
{
    /// <summary>
    /// Methods with the signature 'async void' must be avoided,
    /// because exceptions occurring in those methods cannot be caught and propagate to the SynchronizationContext,
    /// which in terms makes the program crash in totally unrelated places.
    /// Event handlers calling into async code however must have a signature of 'void'.
    /// In those cases the recommendation to avoid the mentioned headaches is to use this method to safely turn
    /// an 'async Task' into an 'async void' by wrapping the entire code in a try-block.
    /// Any errors are logged to the provided logger instead of being propagated.
    /// </summary>
    public static async void TaskToVoidSafely(ILogger logger, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in async void handler");
        }
    }
}
