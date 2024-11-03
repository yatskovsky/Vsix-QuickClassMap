using System;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Shell;

namespace QuickClassMap.Helpers
{
    internal static class AsyncHelper
    {
        public static void FireAndForget(Func<Task> asyncAction, Action<Exception> errorHandler = null)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                try
                {
                    await asyncAction();
                }
                catch (Exception ex)
                {
                    errorHandler?.Invoke(ex);
                }
            });
        }
    }
}
