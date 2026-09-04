using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using QuickClassMap.Core.Domain;
using QuickClassMap.Core.Generators;
using QuickClassMap.Core.Helpers;
using QuickClassMap.Core.Roslyn;
using QuickClassMap.Core.Roslyn.Traversal;
using QuickClassMap.VS.Helpers;

namespace QuickClassMap.VS.Services
{
    internal sealed class ClassMapGenerator
    {
        private const string ExtensionTitle = "Quick Class Map";

        private readonly AsyncPackage package;

        public ClassMapGenerator(AsyncPackage package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
        }

        public void Generate()
        {
            StartGeneration((documentParser, selectedDocuments, progress, cancellationToken) =>
                documentParser.ParseAsync(selectedDocuments, progress, cancellationToken));
        }

        public void GenerateByWalking(bool walkDown, int depth)
        {
            StartGeneration((documentParser, selectedDocuments, progress, cancellationToken) =>
            {
                var options = new ClassGraphTraversalOptions { MaxDepth = depth };
                return walkDown
                    ? documentParser.ParseWithWalkDownAsync(
                        selectedDocuments,
                        options,
                        progress,
                        cancellationToken)
                    : documentParser.ParseWithWalkUpAsync(
                        selectedDocuments,
                        options,
                        progress,
                        cancellationToken);
            });
        }

        private void StartGeneration(
            Func<RoslynDocumentParser, List<string>, IProgress<int>, CancellationToken, Task<List<ClassInfo>>> parse)
        {
            AsyncHelper.FireAndForget(package, () => GenerateDiagramAsync(parse), HandleGenerationError);
        }

        private async Task GenerateDiagramAsync(
            Func<RoslynDocumentParser, List<string>, IProgress<int>, CancellationToken, Task<List<ClassInfo>>> parse)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            await KnownUIContexts.SolutionExistsAndFullyLoadedContext;

            var statusBarService = new StatusBarService(package);
            var statusBarCancellation = new CancellationTokenSource();
            try
            {
                statusBarService.ShowProgress("Generating diagram: collect selected documents...", 0);

                var docRetrievalService = new DocumentRetrievalService(package);
                var selectedDocuments = docRetrievalService.GetSelectedDocuments();
                if (selectedDocuments.Count == 0)
                {
                    throw new InfoException("No C# classes are selected.");
                }

                statusBarService.ShowProgress("Generating diagram: initialize parser...", 0);

                var workspaceProvider = new WorkspaceProvider(package);
                var workspace = await workspaceProvider.GetWorkspaceAsync();
                var solutionDirectory = workspaceProvider.GetSolutionDirectory(workspace);
                var documentParser = new RoslynDocumentParser(workspace);
                var progress = new Progress<int>(UpdateProgress);
                var classInfos = await parse(
                    documentParser,
                    selectedDocuments,
                    progress,
                    package.DisposalToken);

                statusBarService.ShowProgress("Generating diagram: generate output...", 0);

                var dgmlClassDiagram = new DgmlClassDiagramGenerator(documentParser.DefaultNamespace, solutionDirectory)
                    .Generate(classInfos);

                var docCreationService = new DocumentCreationService(package);
                docCreationService.CreateDgmlDocumentWithContent(dgmlClassDiagram);
            }
            finally
            {
                statusBarCancellation.Cancel();
                statusBarService.HideProgress();
            }

            void UpdateProgress(int percent)
            {
                AsyncHelper.FireAndForget(package, async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    if (!statusBarCancellation.IsCancellationRequested)
                    {
                        statusBarService.ShowProgress("Generating diagram: parse documents...", percent);
                    }
                });
            }
        }

        private void HandleGenerationError(Exception exception)
        {
            ActivityLog.LogError(GetType().FullName, exception.ToString());

            if (exception is InfoException)
            {
                VsShellUtilities.ShowMessageBox(
                    package,
                    exception.Message,
                    ExtensionTitle,
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            else
            {
                VsShellUtilities.ShowMessageBox(
                    package,
                    $"An error occurred: {exception.Message}",
                    ExtensionTitle,
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
    }
}
