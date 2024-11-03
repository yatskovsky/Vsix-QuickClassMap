using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using QuickClassMap.Domain;
using QuickClassMap.Helpers;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;

namespace QuickClassMap.Roslyn
{
    internal class RoslynDocumentParser
    {
        private readonly IAsyncServiceProvider _serviceProvider;

        private Project _project;
        private Compilation _compilation;

        public RoslynDocumentParser(IAsyncServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public Namespace DefaultNamespace { get; private set; }

        public async Task<List<ClassInfo>> ParseAsync(List<string> filePaths, IProgress<int> progressAction)
        {
            VisualStudioWorkspace workspace = await GetVisualStudioWorkspaceAsync();
            var symbolToClassInfoMap = new Dictionary<INamedTypeSymbol, ClassInfo>(SymbolEqualityComparer.Default);
            var classParser = new RoslynClassParser(symbolToClassInfoMap);

            await InitializeProjectAndCompilationAsync(workspace, filePaths.FirstOrDefault());

            var progressTracker = new ProgressTracker(progressAction, filePaths.Count);
            foreach (var filePath in filePaths)
            {
                await ProcessFileAsync(workspace, filePath, classParser);

                progressTracker.Increment();
            }

            var relationshipParser = new RoslynRelationshipParser(_compilation, symbolToClassInfoMap);
            relationshipParser.ProcessRelationships();

            return symbolToClassInfoMap.Values.ToList();
        }

        private async Task InitializeProjectAndCompilationAsync(VisualStudioWorkspace workspace, string filePath)
        {
            var documentId = workspace.CurrentSolution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault()
                    ?? throw new ArgumentException($"Document not found in the current solution: {filePath}");

            var document = workspace.CurrentSolution.GetDocument(documentId);

            _project = document.Project;
            _compilation = await _project.GetCompilationAsync();
            DefaultNamespace = new Namespace(_project.DefaultNamespace);
        }

        private async Task ProcessFileAsync(VisualStudioWorkspace workspace, string filePath, RoslynClassParser classParser)
        {
            var documentId = workspace.CurrentSolution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault()
                ?? throw new ArgumentException($"Document not found in the current solution: {filePath}");

            var document = workspace.CurrentSolution.GetDocument(documentId);

            var syntaxTree = await document.GetSyntaxTreeAsync();
            if (!_compilation.ContainsSyntaxTree(syntaxTree))
            {
                // Linked documents are not part of the compilation.
                return;
            }

            var semanticModel = _compilation.GetSemanticModel(syntaxTree);

            classParser.ParseClasses(syntaxTree, semanticModel);
        }

        private async Task<VisualStudioWorkspace> GetVisualStudioWorkspaceAsync()
        {
            var componentModel = await _serviceProvider.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            return componentModel.GetService<VisualStudioWorkspace>();
        }
    }
}
