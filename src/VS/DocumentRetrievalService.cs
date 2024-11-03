using System;
using System.Collections.Generic;
using System.IO;

using EnvDTE;

using EnvDTE80;

using Microsoft.VisualStudio.Shell;

namespace QuickClassMap.VS
{
    internal class DocumentRetrievalService
    {
        private readonly IServiceProvider _serviceProvider;

        public DocumentRetrievalService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        private DTE2 GetDteService()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return (DTE2)_serviceProvider.GetService(typeof(DTE));
        }

        public List<string> GetSelectedDocuments()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = GetDteService();
            var documents = new List<string>();

            foreach (SelectedItem selectedItem in dte.SelectedItems)
            {
                if (selectedItem.IsProject())
                {
                    CollectDocuments(selectedItem.Project.ProjectItems, documents);
                }
                else if (selectedItem.IsFolder())
                {
                    CollectDocuments(selectedItem.ProjectItem.ProjectItems, documents);
                }
                else if (selectedItem.IsFile())
                {
                    AddDocumentIfValid(selectedItem.ProjectItem, documents);
                }
            }

            return documents;
        }

        private void CollectDocuments(ProjectItems projectItems, List<string> documents)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (ProjectItem item in projectItems)
            {
                if (item.Kind == Constants.vsProjectItemKindPhysicalFile)
                {
                    AddDocumentIfValid(item, documents);
                }
                else if (item.Kind == Constants.vsProjectItemKindPhysicalFolder)
                {
                    CollectDocuments(item.ProjectItems, documents);
                }
            }
        }

        private void AddDocumentIfValid(ProjectItem item, List<string> documents)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Note: using FileCodeModel.Language is too slow.
            // item.FileCodeModel?.Language == CodeModelLanguageConstants.vsCMLanguageCSharp
            var fileName = item.FileNames[1];
            if (IsCSharpFile(fileName))
            {
                documents.Add(fileName);
            }
        }

        private bool IsCSharpFile(string fileName)
        {
            return string.Equals(Path.GetExtension(fileName), ".cs", StringComparison.OrdinalIgnoreCase);
        }
    }
}
