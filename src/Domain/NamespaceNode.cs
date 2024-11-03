using System.Collections.Generic;
using System.Linq;

namespace QuickClassMap.Domain
{
    public class NamespaceNode
    {
        public Namespace Namespace { get; }
        public List<ClassInfo> Classes { get; } = new List<ClassInfo>();
        public Dictionary<string, NamespaceNode> Children { get; } = new Dictionary<string, NamespaceNode>();

        public NamespaceNode(Namespace name)
        {
            Namespace = name;
        }

        public static NamespaceNode CreateHierarchy(List<ClassInfo> classes)
        {
            var root = new NamespaceNode(new Namespace(""));

            foreach (var classInfo in classes)
            {
                AddClassToHierarchy(root, classInfo);
            }

            return root;
        }

        private static void AddClassToHierarchy(NamespaceNode root, ClassInfo classInfo)
        {
            var @namespace = new Namespace(classInfo.FullName);
            NamespaceNode currentNode = root;

            foreach (var part in @namespace.Parts.Take(@namespace.Parts.Count - 1))
            {
                if (!currentNode.Children.TryGetValue(part, out var childNode))
                {
                    childNode = new NamespaceNode(currentNode.Namespace.Append(part));
                    currentNode.Children[part] = childNode;
                }
                currentNode = childNode;
            }

            currentNode.Classes.Add(classInfo);
        }
    }
}
