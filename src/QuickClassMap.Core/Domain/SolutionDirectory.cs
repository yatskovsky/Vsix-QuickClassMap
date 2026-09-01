using System;
using System.IO;

namespace QuickClassMap.Core.Domain
{
    public sealed class SolutionDirectory
    {
        private readonly Uri _baseUri;

        public SolutionDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("A solution directory is required.", nameof(directory));
            }

            DirectoryPath = directory;
            var directoryUri = new Uri(directory);
            _baseUri = new Uri(directoryUri.AbsoluteUri.TrimEnd('/') + "/");
        }

        public string DirectoryPath { get; }

        public string GetRelativePath(string filePath)
        {
            var relativeUri = _baseUri.MakeRelativeUri(new Uri(filePath));
            return relativeUri.IsAbsoluteUri
                ? null
                : Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
