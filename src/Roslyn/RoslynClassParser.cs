using QuickClassMap.Domain;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System.Collections.Generic;
using System.Linq;

namespace QuickClassMap.Roslyn
{
    internal class RoslynClassParser
    {
        private readonly Dictionary<INamedTypeSymbol, ClassInfo> _symbolToClassInfoMap;

        public RoslynClassParser(Dictionary<INamedTypeSymbol, ClassInfo> symbolToClassInfoMap)
        {
            _symbolToClassInfoMap = symbolToClassInfoMap;
        }

        public List<ClassInfo> ParseClasses(SyntaxTree syntaxTree, SemanticModel semanticModel)
        {
            var classDeclarations = syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var classDeclaration in classDeclarations)
            {
                if (semanticModel.GetDeclaredSymbol(classDeclaration) is INamedTypeSymbol classSymbol)
                {
                    var classInfo = GenerateClassInfo(classSymbol);
                    _symbolToClassInfoMap[classSymbol] = classInfo;
                }
            }

            var interfaceDeclarations = syntaxTree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>();
            foreach (var interfaceDeclaration in interfaceDeclarations)
            {
                if (semanticModel.GetDeclaredSymbol(interfaceDeclaration) is INamedTypeSymbol interfaceSymbol)
                {
                    var interfaceInfo = GenerateClassInfo(interfaceSymbol, isInterface: true);
                    _symbolToClassInfoMap[interfaceSymbol] = interfaceInfo;
                }
            }

            var recordDeclarations = syntaxTree.GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>();
            foreach (var recordDeclaration in recordDeclarations)
            {
                if (semanticModel.GetDeclaredSymbol(recordDeclaration) is INamedTypeSymbol recordSymbol)
                {
                    var recordInfo = GenerateClassInfo(recordSymbol);
                    _symbolToClassInfoMap[recordSymbol] = recordInfo;
                }
            }

            return _symbolToClassInfoMap.Values.ToList();
        }

        public ClassInfo GenerateClassInfo(INamedTypeSymbol classSymbol, bool isInterface = false)
        {
            var classInfo = new ClassInfo
            {
                FullName = classSymbol.ToDisplayString(),
                Name = classSymbol.Name,
                Relationships = new HashSet<RelationshipInfo>(),
                IsInterface = isInterface
            };

            return classInfo;
        }
    }
}
