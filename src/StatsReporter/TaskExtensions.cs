using System.Diagnostics;

namespace DebugMonitor
{
    /// <summary>
    /// Extension methods for the <see cref="System.Threading.Tasks.Task"/> type.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Ignores the result of a task so it can be used in a true 'fire-and-forget' manner.
        /// </summary>
        /// <param name="task">The task to ignore.</param>
        public static void Ignore(this Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            Task t = task.ContinueWith(
                (Task completedTask) => LogExceptionIfPresent(completedTask.Exception),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        /// <summary>
        /// If an exception happened, log it.
        /// </summary>
        /// <param name="e">The exception to log.</param>
        private static void LogExceptionIfPresent(Exception? e)
        {
            try
            {
                if (e != null)
                {
                    Trace.WriteLine(e);
                }
            }
            catch
            {
                // Since this is used as a continuation we won't observe, we're going to be
                // super paranoid and make sure this doesn't throw.
            }
        }
    }
}
