using QuickClassMap.Core.Domain;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System.Collections.Generic;
using System.Linq;

namespace QuickClassMap.Core.Roslyn.Parsing
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
            return ParseClasses(
                syntaxTree,
                semanticModel,
                DiscoverSourceTypes(syntaxTree, semanticModel).Select(sourceType => sourceType.Symbol));
        }

        public IReadOnlyCollection<SourceTypeInfo> DiscoverSourceTypes(
            SyntaxTree syntaxTree,
            SemanticModel semanticModel)
        {
            var sourceTypes = new Dictionary<INamedTypeSymbol, List<SyntaxReference>>(SymbolEqualityComparer.Default);
            foreach (var declaration in GetTypeDeclarations(syntaxTree))
            {
                if (!(semanticModel.GetDeclaredSymbol(declaration) is INamedTypeSymbol declaredSymbol))
                {
                    continue;
                }

                var symbol = declaredSymbol.OriginalDefinition;
                if (!sourceTypes.TryGetValue(symbol, out var declarations))
                {
                    declarations = new List<SyntaxReference>();
                    sourceTypes.Add(symbol, declarations);
                }

                declarations.Add(declaration.GetReference());
            }

            return sourceTypes
                .Select(pair => new SourceTypeInfo(pair.Key, pair.Value))
                .ToList();
        }

        public IReadOnlyCollection<INamedTypeSymbol> DiscoverSourceSymbols(
            SyntaxTree syntaxTree,
            SemanticModel semanticModel)
        {
            return DiscoverSourceTypes(syntaxTree, semanticModel)
                .Select(sourceType => sourceType.Symbol)
                .ToList();
        }

        public List<ClassInfo> ParseClasses(
            SyntaxTree syntaxTree,
            SemanticModel semanticModel,
            IEnumerable<INamedTypeSymbol> symbols)
        {
            var symbolsToParse = new HashSet<INamedTypeSymbol>(
                symbols.Select(symbol => symbol.OriginalDefinition),
                SymbolEqualityComparer.Default);

            foreach (var sourceType in DiscoverSourceTypes(syntaxTree, semanticModel))
            {
                if (!symbolsToParse.Contains(sourceType.Symbol))
                {
                    continue;
                }

                var classSymbol = sourceType.Symbol;
                _symbolToClassInfoMap[classSymbol] = GenerateClassInfo(
                    classSymbol,
                    isInterface: classSymbol.TypeKind == TypeKind.Interface);
            }

            return _symbolToClassInfoMap.Values.ToList();
        }

        private IEnumerable<SyntaxNode> GetTypeDeclarations(SyntaxTree syntaxTree)
        {
            return syntaxTree.GetRoot().DescendantNodes().Where(node =>
                node is ClassDeclarationSyntax ||
                node is InterfaceDeclarationSyntax ||
                node is RecordDeclarationSyntax recordDeclaration &&
                !recordDeclaration.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword));
        }

        public ClassInfo GenerateClassInfo(INamedTypeSymbol classSymbol, bool isInterface = false)
        {
            var classInfo = new ClassInfo
            {
                FullName = classSymbol.ToDisplayString(),
                Name = classSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), // no namespace, keep generics
                SourceFilePath = classSymbol.Locations.FirstOrDefault(location => location.IsInSource)?.SourceTree?.FilePath,
                Relationships = new HashSet<RelationshipInfo>(),
                IsInterface = isInterface
            };

            return classInfo;
        }
    }
}
