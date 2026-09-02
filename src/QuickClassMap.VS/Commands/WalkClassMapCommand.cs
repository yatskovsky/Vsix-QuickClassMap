using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Shell;

using QuickClassMap.VS.Services;

namespace QuickClassMap.VS.Commands
{
    internal sealed class WalkClassMapCommand
    {
        public const int WalkUpDepth1CommandId = 0x0110;
        public const int WalkUpDepth2CommandId = 0x0111;
        public const int WalkUpDepth3CommandId = 0x0112;
        public const int WalkUpDepth5CommandId = 0x0113;
        public const int WalkUpDepth8CommandId = 0x0114;
        public const int WalkDownDepth1CommandId = 0x0120;
        public const int WalkDownDepth2CommandId = 0x0121;
        public const int WalkDownDepth3CommandId = 0x0122;
        public const int WalkDownDepth5CommandId = 0x0123;
        public const int WalkDownDepth8CommandId = 0x0124;

        public static readonly Guid CommandSet = new Guid("95f2241d-18d3-4cef-aa95-4dae87e9bfd7");

        private readonly ClassMapGenerator classMapGenerator;

        private WalkClassMapCommand(
            ClassMapGenerator classMapGenerator,
            OleMenuCommandService commandService)
        {
            this.classMapGenerator = classMapGenerator ?? throw new ArgumentNullException(nameof(classMapGenerator));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            AddWalkDepthCommands(commandService, walkDown: false);
            AddWalkDepthCommands(commandService, walkDown: true);
        }

        public static WalkClassMapCommand Instance { get; private set; }

        public static async Task InitializeAsync(
            AsyncPackage package,
            ClassMapGenerator classMapGenerator)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new WalkClassMapCommand(classMapGenerator, commandService);
        }

        private void AddWalkDepthCommands(OleMenuCommandService commandService, bool walkDown)
        {
            var commandIds = walkDown
                ? new[]
                {
                    WalkDownDepth1CommandId,
                    WalkDownDepth2CommandId,
                    WalkDownDepth3CommandId,
                    WalkDownDepth5CommandId,
                    WalkDownDepth8CommandId
                }
                : new[]
                {
                    WalkUpDepth1CommandId,
                    WalkUpDepth2CommandId,
                    WalkUpDepth3CommandId,
                    WalkUpDepth5CommandId,
                    WalkUpDepth8CommandId
                };
            var depths = new[] { 1, 2, 3, 5, 8 };

            for (var index = 0; index < commandIds.Length; index++)
            {
                var commandId = new CommandID(CommandSet, commandIds[index]);
                var depth = depths[index];
                commandService.AddCommand(new MenuCommand(
                    (sender, e) => OnWalk(walkDown, depth),
                    commandId));
            }
        }

        private void OnWalk(bool walkDown, int depth)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            classMapGenerator.GenerateByWalking(walkDown, depth);
        }
    }
}
