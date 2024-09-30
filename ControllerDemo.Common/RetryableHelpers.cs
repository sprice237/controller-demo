using System;
using System.IO;
using System.Threading.Tasks;

namespace ControllerDemo.Common
{

    public static class RetryableHelpers
    {

        public class ExhaustedRetriesException : Exception
        {
            public ExhaustedRetriesException(Exception e) : base("Exhausted retries attempting to open FileStream", e) { }
        }

        public static async Task<T> GetWithRetry<T>(Func<T> getter, Func<Exception, bool> isRetryableException = null, int numRetries = 10, int retryTimeout = 500) where T : class
        {
            isRetryableException ??= e => true;

            T thing = null;

            var numAttemptsRemaining = Math.Max(numRetries, 1);
            while (numAttemptsRemaining > 0 && thing == null)
            {
                try
                {
                    thing = getter();
                }
                catch (Exception e)
                {
                    if (!isRetryableException(e))
                    {
                        throw;
                    }

                    --numAttemptsRemaining;
                    if (numAttemptsRemaining == 0)
                    {
                        throw new ExhaustedRetriesException(e);
                    }

                    await Task.Delay(retryTimeout);
                }
            }

            return thing;
        }

        public static async Task DoWithRetry(Action action, Func<Exception, bool> isRetryableException = null, int numRetries = 10, int retryTimeout = 500)
        {
            var wrappedAction = new Func<object>(() =>
            {
                action();
                return null;
            });
            await GetWithRetry(wrappedAction, isRetryableException, numRetries, retryTimeout);
        }
    }
}