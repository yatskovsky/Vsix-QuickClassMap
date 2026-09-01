using System;
using System.IO;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;

using QuickClassMap.Domain;

namespace QuickClassMap.VS
{
    internal class WorkspaceProvider
    {
        private readonly IAsyncServiceProvider _serviceProvider;

        public WorkspaceProvider(IAsyncServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task<Workspace> GetWorkspaceAsync()
        {
            var componentModel = await _serviceProvider.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            return componentModel.GetService<VisualStudioWorkspace>();
        }

        public SolutionDirectory GetSolutionDirectory(Workspace workspace)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            var solutionFilePath = workspace.CurrentSolution.FilePath;
            if (string.IsNullOrEmpty(solutionFilePath))
            {
                throw new InvalidOperationException("The current workspace has no solution.");
            }

            return new SolutionDirectory(Path.GetDirectoryName(solutionFilePath));
        }
    }
}
