using System;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace QuickClassMap.VS.Helpers
{
    internal static class AsyncHelper
    {
        public static void FireAndForget(
            AsyncPackage package,
            Func<Task> asyncAction,
            Action<Exception>? errorHandler = null)
        {
            package.JoinableTaskFactory.RunAsync(async delegate
            {
                try
                {
                    await asyncAction();
                }
                catch (Exception ex)
                {
                    errorHandler?.Invoke(ex);
                }
            }).Task.Forget();
        }
    }
}
