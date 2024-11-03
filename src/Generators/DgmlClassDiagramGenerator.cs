using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using QuickClassMap.Domain;

namespace QuickClassMap.Generators
{
    public class DgmlClassDiagramGenerator
    {
        private static readonly XNamespace _ns = "http://schemas.microsoft.com/vs/2009/dgml";

        private readonly Namespace _defaultNamespace;

        public DgmlClassDiagramGenerator(Namespace defaultNamespace)
        {
            _defaultNamespace = defaultNamespace;
        }

        public string Generate(List<ClassInfo> classes)
        {
            var hierarchyRoot = NamespaceNode.CreateHierarchy(classes);

            XElement root = new XElement(_ns + "DirectedGraph",
                new XElement(_ns + "Nodes", GenerateDgmlNodes(hierarchyRoot)),
                new XElement(_ns + "Links", GenerateContainsLinks(hierarchyRoot), AddAllRelationships(classes)),
                AddCategories(),
                AddStyles()
            );

            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                root
            );

            return doc.Declaration + Environment.NewLine + doc.Root;
        }

        private IEnumerable<XElement> GenerateDgmlNodes(NamespaceNode root)
        {
            return GenerateNodesRecursive(root);
        }

        private IEnumerable<XElement> GenerateNodesRecursive(NamespaceNode node)
        {
            string currentNamespace = node.Namespace.FullName;

            if (node.Classes.Any())
            {
                yield return new XElement(_ns + "Node",
                    new XAttribute("Id", FormatNamespace(currentNamespace)),
                    new XAttribute("Group", "Expanded"),
                    new XAttribute("Label", FormatNamespace(currentNamespace)),
                    new XAttribute("Category", "CodeSchema_Namespace")
                );
            }

            foreach (var classInfo in node.Classes)
            {
                yield return new XElement(_ns + "Node",
                    new XAttribute("Id", FormatClassName(classInfo.FullName)),
                    new XAttribute("Label", FormatClassName(classInfo.Name)),
                    new XAttribute("Category", classInfo.IsInterface ? "CodeSchema_Interface" : "CodeSchema_Class"),
                    !string.IsNullOrEmpty(currentNamespace) ? new XAttribute("Group", FormatNamespace(currentNamespace)) : null
                );
            }

            foreach (var child in node.Children.Values)
            {
                foreach (var element in GenerateNodesRecursive(child))
                {
                    yield return element;
                }
            }
        }

        private IEnumerable<XElement> GenerateContainsLinks(NamespaceNode root)
        {
            return GenerateContainsLinksRecursive(root);
        }

        private IEnumerable<XElement> GenerateContainsLinksRecursive(NamespaceNode node)
        {
            string currentNamespace = node.Namespace.FullName;

            if (node.Classes.Any())
            {
                foreach (var classInfo in node.Classes)
                {
                    yield return new XElement(_ns + "Link",
                        new XAttribute("Source", FormatNamespace(currentNamespace)),
                        new XAttribute("Target", FormatClassName(classInfo.FullName)),
                        new XAttribute("Category", "Contains")
                    );
                }
            }

            foreach (var child in node.Children.Values)
            {
                if (child.Classes.Any() && node.Classes.Any())
                {
                    yield return new XElement(_ns + "Link",
                        new XAttribute("Source", FormatNamespace(currentNamespace)),
                        new XAttribute("Target", FormatNamespace(child.Namespace.FullName)),
                        new XAttribute("Category", "Contains")
                    );
                }

                foreach (var element in GenerateContainsLinksRecursive(child))
                {
                    yield return element;
                }
            }
        }

        private IEnumerable<XElement> AddAllRelationships(List<ClassInfo> classes)
        {
            List<XElement> relationships = new List<XElement>();

            foreach (var classInfo in classes)
            {
                foreach (var relationship in classInfo.Relationships)
                {
                    XElement link = new XElement(_ns + "Link",
                        new XAttribute("Source", FormatClassName(classInfo.FullName)),
                        new XAttribute("Target", FormatClassName(relationship.RelatedClassName)),
                        new XAttribute("Category", relationship.Type.ToString())
                    );
                    relationships.Add(link);
                }
            }

            return relationships;
        }

        private XElement AddCategories()
        {
            return new XElement(_ns + "Categories",
                new XElement(_ns + "Category", new XAttribute("Id", "Contains")),
                new XElement(_ns + "Category", new XAttribute("Id", "Inheritance"), new XAttribute("Background", "#FF00FF00")),
                new XElement(_ns + "Category", new XAttribute("Id", "Implementation"), new XAttribute("Background", "#FFFFFF00")),
                new XElement(_ns + "Category", new XAttribute("Id", "Composes"), new XAttribute("Background", "#FF008000")),
                new XElement(_ns + "Category", new XAttribute("Id", "Aggregates"), new XAttribute("Background", "#FFFFA500")),
                new XElement(_ns + "Category", new XAttribute("Id", "Uses"), new XAttribute("Background", "#FF000000"))
            );
        }

        private XElement AddStyles()
        {
            return new XElement(_ns + "Styles",
                AddStyleForRelationship(nameof(RelationshipType.Inherits), "#FF000000", "", "2"),
                AddStyleForRelationship(nameof(RelationshipType.Implements), "#FF000000", "4,2", "2"),
                AddStyleForRelationship(nameof(RelationshipType.Composes), "#FFFFA500", "", "1"),
                AddStyleForRelationship(nameof(RelationshipType.Aggregates), "#FFFFA500", "2,4", "1"),
                AddStyleForRelationship(nameof(RelationshipType.Uses), "#FF000000", "1,2", "1")
            );
        }

        private XElement AddStyleForRelationship(string category, string color, string dashArray, string thickness)
        {
            XElement style = new XElement(_ns + "Style",
                new XAttribute("TargetType", "Link"),
                new XAttribute("GroupLabel", category),
                new XAttribute("ValueLabel", "True"),
                new XElement(_ns + "Condition", new XAttribute("Expression", $"HasCategory('{category}')")),
                new XElement(_ns + "Setter", new XAttribute("Property", "Stroke"), new XAttribute("Value", color)),
                new XElement(_ns + "Setter", new XAttribute("Property", "StrokeThickness"), new XAttribute("Value", thickness))
            );

            if (!string.IsNullOrEmpty(dashArray))
            {
                style.Add(new XElement(_ns + "Setter",
                    new XAttribute("Property", "StrokeDashArray"),
                    new XAttribute("Value", dashArray)));
            }

            return style;
        }

        private string FormatClassName(string name)
        {
            return name;

            //return _defaultNamespace.StripNamespace(name);
        }

        private string FormatNamespace(string fullName)
        {
            return _defaultNamespace.StripNamespace(fullName);
        }
    }
}
