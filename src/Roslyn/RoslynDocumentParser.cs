using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using QuickClassMap.Domain;
using QuickClassMap.Helpers;

using Microsoft.CodeAnalysis;

namespace QuickClassMap.Roslyn
{
    internal class RoslynDocumentParser
    {
        private readonly Workspace _workspace;

        private Project _project;
        private Compilation _compilation;

        public RoslynDocumentParser(Workspace workspace)
        {
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        }

        public Namespace DefaultNamespace { get; private set; }

        public async Task<List<ClassInfo>> ParseAsync(List<string> filePaths, IProgress<int> progressAction)
        {
            var symbolToClassInfoMap = new Dictionary<INamedTypeSymbol, ClassInfo>(SymbolEqualityComparer.Default);
            var classParser = new RoslynClassParser(symbolToClassInfoMap);

            await InitializeProjectAndCompilationAsync(filePaths.FirstOrDefault());

            var progressTracker = new ProgressTracker(progressAction, filePaths.Count);
            foreach (var filePath in filePaths)
            {
                await ProcessFileAsync(filePath, classParser);

                progressTracker.Increment();
            }

            var relationshipParser = new RoslynRelationshipParser(_compilation, symbolToClassInfoMap);
            relationshipParser.ProcessRelationships();

            return symbolToClassInfoMap.Values.ToList();
        }

        private async Task InitializeProjectAndCompilationAsync(string filePath)
        {
            var documentId = _workspace.CurrentSolution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault()
                    ?? throw new ArgumentException($"Document not found in the current solution: {filePath}");

            var document = _workspace.CurrentSolution.GetDocument(documentId);

            _project = document.Project;
            _compilation = await _project.GetCompilationAsync();
            DefaultNamespace = new Namespace(_project.DefaultNamespace);
        }

        private async Task ProcessFileAsync(string filePath, RoslynClassParser classParser)
        {
            var documentId = _workspace.CurrentSolution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault()
                ?? throw new ArgumentException($"Document not found in the current solution: {filePath}");

            var document = _workspace.CurrentSolution.GetDocument(documentId);

            var syntaxTree = await document.GetSyntaxTreeAsync();
            if (!_compilation.ContainsSyntaxTree(syntaxTree))
            {
                // Linked documents are not part of the compilation.
                return;
            }

            var semanticModel = _compilation.GetSemanticModel(syntaxTree);

            classParser.ParseClasses(syntaxTree, semanticModel);
        }

    }
}
