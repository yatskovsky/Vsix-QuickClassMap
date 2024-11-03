using System;
using System.ComponentModel.Design;
using System.Threading;
using System.Threading.Tasks;

using QuickClassMap.Generators;
using QuickClassMap.Helpers;
using QuickClassMap.Roslyn;
using QuickClassMap.VS;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace QuickClassMap
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class GenerateClassMapCommand
    {
        private const string ExtensionTitle = "Quick Class Map";

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("95f2241d-18d3-4cef-aa95-4dae87e9bfd7");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateClassMapCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private GenerateClassMapCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static GenerateClassMapCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider AsyncServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new GenerateClassMapCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            AsyncHelper.FireAndForget(ExecuteAsync, (ex) =>
            {
                ActivityLog.LogError(GetType().FullName, ex.ToString());

                if (ex is InfoException)
                {
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        $"{ex.Message}",
                        ExtensionTitle,
                        OLEMSGICON.OLEMSGICON_INFO,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                }
                else
                {
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        $"An error occurred: {ex.Message}",
                        ExtensionTitle,
                        OLEMSGICON.OLEMSGICON_CRITICAL,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                }
            });
        }

        private async Task ExecuteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var statusBarService = new StatusBarService(ServiceProvider);
            var statusBarCancellation = new CancellationTokenSource();
            try
            {
                // Collect selected documents
                statusBarService.ShowProgress("Generating diagram: collect selected documents...", 0);

                var docRetrievalService = new DocumentRetrievalService(ServiceProvider);

                var selectedDocuments = docRetrievalService.GetSelectedDocuments();
                if (selectedDocuments.Count == 0)
                {
                    throw new InfoException("No C# classes are selected.");
                }

                // Collect class info from the selected documents
                statusBarService.ShowProgress("Generating diagram: initialize parser...", 0);

                var documentParser = new RoslynDocumentParser(AsyncServiceProvider);
                var classInfos = await documentParser.ParseAsync(selectedDocuments, new Progress<int>(UpdateProgress));

                // Generate class diagrams
                statusBarService.ShowProgress("Generating diagram: generate output...", 0);

                var dgmlClassDiagram = new DgmlClassDiagramGenerator(documentParser.DefaultNamespace)
                     .Generate(classInfos);

                var docCreationService = new DocumentCreationService(ServiceProvider);
                docCreationService.CreateDgmlDocumentWithContent(dgmlClassDiagram);
            }
            finally
            {
                statusBarCancellation.Cancel();
                statusBarService.HideProgress();
            }

            void UpdateProgress(int percent)
            {
                AsyncHelper.FireAndForget(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    if (!statusBarCancellation.IsCancellationRequested)
                    {
                        statusBarService.ShowProgress($"Generating diagram: parse documents...", percent);

                    }
                });
            }
        }
    }
}
