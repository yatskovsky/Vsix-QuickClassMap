using Basic.Reference.Assemblies;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using QuickClassMap.Core.Domain;
using QuickClassMap.Core.Roslyn;

namespace QuickClassMap.Tests;

public sealed class RoslynParserFixture
{
    public List<ClassInfo> Parse(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references: Net472.References.All,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var symbolToClassInfoMap = new Dictionary<INamedTypeSymbol, ClassInfo>(SymbolEqualityComparer.Default);
        var classParser = new RoslynClassParser(symbolToClassInfoMap);

        var classInfos = classParser.ParseClasses(syntaxTree, semanticModel);

        var relationshipParser = new RoslynRelationshipParser(compilation, symbolToClassInfoMap);
        relationshipParser.ProcessRelationships();

        return classInfos;
    }
}
