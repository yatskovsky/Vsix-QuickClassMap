using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Shell;

using QuickClassMap.VS.Services;

namespace QuickClassMap.VS.Commands
{
    internal sealed class GenerateClassMapCommand
    {
        public const int CommandId = 0x0100;

        public static readonly Guid CommandSet = new Guid("95f2241d-18d3-4cef-aa95-4dae87e9bfd7");

        private readonly ClassMapGenerator classMapGenerator;

        private GenerateClassMapCommand(
            ClassMapGenerator classMapGenerator,
            OleMenuCommandService commandService)
        {
            this.classMapGenerator = classMapGenerator ?? throw new ArgumentNullException(nameof(classMapGenerator));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var commandId = new CommandID(CommandSet, CommandId);
            commandService.AddCommand(new MenuCommand(OnGenerateClassMap, commandId));
        }

        public static GenerateClassMapCommand Instance { get; private set; } = null!;

        public static async Task InitializeAsync(
            AsyncPackage package,
            ClassMapGenerator classMapGenerator)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService
                ?? throw new InvalidOperationException("The menu command service is unavailable.");
            Instance = new GenerateClassMapCommand(classMapGenerator, commandService);
        }

        private void OnGenerateClassMap(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            classMapGenerator.Generate();
        }
    }
}
