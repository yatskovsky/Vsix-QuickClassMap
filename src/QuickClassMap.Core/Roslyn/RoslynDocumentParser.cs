using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using QuickClassMap.Core.Domain;
using QuickClassMap.Core.Helpers;
using QuickClassMap.Core.Roslyn.Parsing;
using QuickClassMap.Core.Roslyn.Traversal;

using Microsoft.CodeAnalysis;

namespace QuickClassMap.Core.Roslyn
{
    public class RoslynDocumentParser
    {
        private readonly Workspace _workspace;

        private Project _project;
        private Compilation _compilation;

        public RoslynDocumentParser(Workspace workspace)
        {
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        }

        public Namespace DefaultNamespace { get; private set; }

        public Task<List<ClassInfo>> ParseAsync(List<string> filePaths, IProgress<int> progressAction)
        {
            return ParseAsync(filePaths, progressAction, CancellationToken.None);
        }

        public async Task<List<ClassInfo>> ParseAsync(
            List<string> filePaths,
            IProgress<int> progressAction,
            CancellationToken cancellationToken)
        {
            await InitializeProjectAndCompilationAsync(filePaths.FirstOrDefault(), cancellationToken);
            return await ParseFilesAsync(filePaths, progressAction, cancellationToken);
        }

        public async Task<List<ClassInfo>> ParseWithWalkDownAsync(
            List<string> seedFilePaths,
            ClassGraphTraversalOptions options,
            IProgress<int> progressAction,
            CancellationToken cancellationToken)
        {
            return await ParseWithDirectionAsync(
                seedFilePaths,
                options,
                ClassGraphTraversalDirection.Down,
                progressAction,
                cancellationToken);
        }

        public async Task<List<ClassInfo>> ParseWithWalkUpAsync(
            List<string> seedFilePaths,
            ClassGraphTraversalOptions options,
            IProgress<int> progressAction,
            CancellationToken cancellationToken)
        {
            return await ParseWithDirectionAsync(
                seedFilePaths,
                options,
                ClassGraphTraversalDirection.Up,
                progressAction,
                cancellationToken);
        }

        private async Task<List<ClassInfo>> ParseWithDirectionAsync(
            List<string> seedFilePaths,
            ClassGraphTraversalOptions options,
            ClassGraphTraversalDirection direction,
            IProgress<int> progressAction,
            CancellationToken cancellationToken)
        {
            await InitializeProjectAndCompilationAsync(seedFilePaths.FirstOrDefault(), cancellationToken);

            var seedSymbols = await DiscoverSeedSymbolsAsync(seedFilePaths, cancellationToken);
            var sourceTypeLookup = direction == ClassGraphTraversalDirection.Up
                ? SourceTypeLookup.Create(_compilation)
                : null;
            var graphWalker = new ClassGraphWalker(_compilation, sourceTypeLookup);
            var traversalResult = await graphWalker.WalkAsync(
                seedSymbols,
                options,
                direction,
                cancellationToken);

            // Keep symbol selection separate from file parsing so shared files cannot broaden the result.
            return await ParseFilesAsync(
                GetSourceFilePaths(traversalResult.Symbols),
                progressAction,
                cancellationToken,
                traversalResult.Symbols);
        }

        private async Task<List<ClassInfo>> ParseFilesAsync(
            IReadOnlyCollection<string> filePaths,
            IProgress<int> progressAction,
            CancellationToken cancellationToken,
            IReadOnlyCollection<INamedTypeSymbol> symbolsToParse = null)
        {
            var symbolToClassInfoMap = new Dictionary<INamedTypeSymbol, ClassInfo>(SymbolEqualityComparer.Default);
            var classParser = new RoslynClassParser(symbolToClassInfoMap);

            var progressTracker = new ProgressTracker(progressAction, filePaths.Count);
            foreach (var filePath in filePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ProcessFileAsync(filePath, classParser, symbolsToParse, cancellationToken);

                progressTracker.Increment();
            }

            var relationshipParser = new RoslynRelationshipParser(_compilation, symbolToClassInfoMap);
            relationshipParser.ProcessRelationships();

            return symbolToClassInfoMap.Values.ToList();
        }

        private async Task InitializeProjectAndCompilationAsync(string filePath, CancellationToken cancellationToken)
        {
            var document = GetDocument(filePath);
            _project = document.Project;
            _compilation = await _project.GetCompilationAsync(cancellationToken);
            DefaultNamespace = new Namespace(_project.DefaultNamespace);
        }

        private async Task<IReadOnlyCollection<INamedTypeSymbol>> DiscoverSeedSymbolsAsync(
            IReadOnlyCollection<string> filePaths,
            CancellationToken cancellationToken)
        {
            var symbols = new List<INamedTypeSymbol>();
            var classParser = new RoslynClassParser(
                new Dictionary<INamedTypeSymbol, ClassInfo>(SymbolEqualityComparer.Default));

            foreach (var filePath in filePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var document = GetDocument(filePath);
                var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
                if (syntaxTree == null || !_compilation.ContainsSyntaxTree(syntaxTree))
                {
                    continue;
                }

                var semanticModel = _compilation.GetSemanticModel(syntaxTree);
                symbols.AddRange(classParser.DiscoverSourceSymbols(syntaxTree, semanticModel));
            }

            return symbols;
        }

        private IReadOnlyCollection<string> GetSourceFilePaths(IEnumerable<INamedTypeSymbol> symbols)
        {
            return symbols
                .SelectMany(symbol => symbol.Locations)
                .Where(location =>
                    location.IsInSource &&
                    location.SourceTree != null &&
                    _compilation.ContainsSyntaxTree(location.SourceTree))
                .Select(location => location.SourceTree.FilePath)
                .Where(filePath => !string.IsNullOrEmpty(filePath))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task ProcessFileAsync(
            string filePath,
            RoslynClassParser classParser,
            IReadOnlyCollection<INamedTypeSymbol> symbolsToParse,
            CancellationToken cancellationToken)
        {
            var document = GetDocument(filePath);

            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
            if (!_compilation.ContainsSyntaxTree(syntaxTree))
            {
                // Linked documents are not part of the compilation.
                return;
            }

            var semanticModel = _compilation.GetSemanticModel(syntaxTree);

            if (symbolsToParse == null)
            {
                classParser.ParseClasses(syntaxTree, semanticModel);
            }
            else
            {
                classParser.ParseClasses(syntaxTree, semanticModel, symbolsToParse);
            }
        }

        private Document GetDocument(string filePath)
        {
            var documentId = _workspace.CurrentSolution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault()
                ?? throw new ArgumentException($"Document not found in the current solution: {filePath}", nameof(filePath));

            return _workspace.CurrentSolution.GetDocument(documentId);
        }

    }
}
