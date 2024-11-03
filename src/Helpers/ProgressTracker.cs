using System;

namespace QuickClassMap.Helpers
{
    public class ProgressTracker
    {
        private readonly IProgress<int> _progress;
        private readonly int _total;
        private int _current;
        private int _lastReported;
        private readonly int _reportThreshold;

        public ProgressTracker(IProgress<int> progressAction, int total, int reportThresholdPercent = 10)
        {
            _progress = progressAction;
            _total = total;
            _reportThreshold = reportThresholdPercent;
        }

        public void Increment()
        {
            _current++;

            int currentProgress = (int)((float)_current / _total * 100);

            if (currentProgress - _lastReported >= _reportThreshold || _current == _total)
            {
                _lastReported = currentProgress;
                _progress?.Report(_lastReported);
            }
        }
    }
}
