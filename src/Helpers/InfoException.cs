using System;

namespace QuickClassMap.Helpers
{
    public class InfoException : Exception
    {
        public InfoException() : base()
        {
        }

        public InfoException(string message) : base(message)
        {
        }

        public InfoException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
