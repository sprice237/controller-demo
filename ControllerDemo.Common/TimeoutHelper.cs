using System;
using System.Threading;
using System.Threading.Tasks;

namespace ControllerDemo.Common
{
    public static class TimeoutHelper
    {
        public static Task RunWithTimeout(Func<CancellationToken, Task> taskReturningFunction, int timeoutMilliseconds, bool cancelTaskOnTimeout = true)
        {
            return RunWithTimeoutInternal(taskReturningFunction, timeoutMilliseconds, cancelTaskOnTimeout);
        }

        public static Task<T> RunWithTimeout<T>(Func<CancellationToken, Task<T>> taskReturningFunction, int timeoutMilliseconds, bool cancelTaskOnTimeout = true)
        {
            return (Task<T>)RunWithTimeoutInternal(taskReturningFunction, timeoutMilliseconds, cancelTaskOnTimeout);
        }

        private static Task RunWithTimeoutInternal(Func<CancellationToken, Task> taskReturningFunction, int timeoutMilliseconds, bool cancelTaskOnTimeout = true)
        {
            var taskCancellationTokenSource = new CancellationTokenSource();
            var timeoutCancellationTokenSource = new CancellationTokenSource();
            var task = taskReturningFunction(taskCancellationTokenSource.Token);
            var timeoutTask = Task.Delay(timeoutMilliseconds, timeoutCancellationTokenSource.Token);
            var completedTask = Task.WhenAny(task, timeoutTask).Result;

            if (completedTask == task)
            {
                timeoutCancellationTokenSource.Cancel();
                return task;
            }

            if (cancelTaskOnTimeout)
            {
                taskCancellationTokenSource.Cancel();
            }

            throw new TimeoutException();
        }
    }
}