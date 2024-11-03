using EnvDTE;

namespace QuickClassMap.VS
{
    public static class SelectedItemExtensions
    {
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
        public static bool IsProject(this SelectedItem item)
        {
            return item.Project != null;
        }

        public static bool IsFolder(this SelectedItem item)
        {
            return item.ProjectItem != null &&
                   item.ProjectItem.Kind == Constants.vsProjectItemKindPhysicalFolder;
        }

        public static bool IsFile(this SelectedItem item)
        {
            return item.ProjectItem != null &&
                   item.ProjectItem.Kind == Constants.vsProjectItemKindPhysicalFile;
        }
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
    }
}
