using System;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QuickClassMap.VS
{
    internal class StatusBarService
    {
        private readonly IServiceProvider _serviceProvider;

        private IVsStatusbar _statusBar;
        private uint _progressCookie = 0;

        public StatusBarService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        private IVsStatusbar StatusBar
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (_statusBar == null)
                {
                    _statusBar = (IVsStatusbar)_serviceProvider.GetService(typeof(SVsStatusbar));
                }
                return _statusBar;
            }
        }

        public void ShowProgress(string message, int percent)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            StatusBar?.Progress(ref _progressCookie, 1, message, (uint)percent, 100);
        }

        public void HideProgress()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            StatusBar?.Progress(ref _progressCookie, 0, string.Empty, 0, 0);
        }
    }
}
