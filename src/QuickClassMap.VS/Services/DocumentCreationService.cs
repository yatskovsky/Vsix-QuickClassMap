using System;
using System.IO;

using EnvDTE;

using EnvDTE80;

using Microsoft.VisualStudio.Shell;

namespace QuickClassMap.VS
{
    internal class DocumentCreationService
    {
        private readonly IServiceProvider _serviceProvider;

        public DocumentCreationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        private DTE2 GetDteService()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return (DTE2)_serviceProvider.GetService(typeof(DTE));
        }

        public void CreateTextDocumentWithContent(string content, string docType = @"General\Text File")
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = GetDteService();
            dte.ItemOperations.NewFile(docType);

            Document activeDocument = dte.ActiveDocument;
            if (activeDocument == null)
            {
                throw new InvalidOperationException("No active document found.");
            }

            var textDoc = (TextDocument)activeDocument.Object("TextDocument");
            if (textDoc == null)
            {
                throw new InvalidOperationException("Active document is not a text document.");
            }

            EditPoint editPoint = textDoc.StartPoint.CreateEditPoint();
            editPoint.Insert(content);
        }

        public void CreateDgmlDocumentWithContent(string content)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Save to a temp file
            string tempFilePath = CreateTempDgmlFileName();
            File.WriteAllText(tempFilePath, content);

            // Open the file in Viewer mode
            var dte = GetDteService();
            try
            {
                dte.ItemOperations.OpenFile(tempFilePath);
            }
            catch
            {
                throw new Exception($"Error opening {tempFilePath}");
            }
        }

        private string CreateTempDgmlFileName()
        {
            var tempPath = Path.GetTempPath();
            var tempFileName = Path.GetRandomFileName();
            var tempDgmlFileName = Path.ChangeExtension(tempFileName, ".dgml");
            return Path.Combine(tempPath, tempDgmlFileName);
        }
    }
}
