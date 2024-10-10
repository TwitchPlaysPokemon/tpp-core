using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Utils;

public static class TaskUtils
{
    /// Same as <see cref="Task.WhenAll(System.Collections.Generic.IEnumerable{System.Threading.Tasks.Task})"/>,
    /// but throws a <exception cref="TaskCanceledException"></exception> as soon as any of the tasks is cancelled,
    /// or throws an exception as soon as any of the tasks throws (it rethrows that task's aggregate exception).
    /// This is useful if you want to stop waiting for other tasks as soon as one of these conditions occur.
    public static async Task WhenAllFastExit(IEnumerable<Task> tasks)
    {
        await WhenAllFastExit(tasks.ToArray());
    }

    /// Same as <see cref="Task.WhenAll(System.Collections.Generic.IEnumerable{System.Threading.Tasks.Task})"/>,
    /// but throws a <exception cref="TaskCanceledException"></exception> as soon as any of the tasks is cancelled,
    /// or throws an exception as soon as any of the tasks throws (it rethrows that task's aggregate exception).
    /// This is useful if you want to stop waiting for other tasks as soon as one of these conditions occur.
    public static async Task WhenAllFastExit(params Task[] tasks)
    {
        List<Task> remainingTasks = tasks.ToList();
        while (remainingTasks.Count > 0)
        {
            Task finishedTask = await Task.WhenAny(remainingTasks);
            if (finishedTask.IsCanceled)
                throw new TaskCanceledException();
            if (finishedTask.IsFaulted)
                throw finishedTask.Exception;
            remainingTasks.Remove(finishedTask);
        }
    }
}
