using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using QuickClassMap.Core.Domain;
using QuickClassMap.Core.Roslyn;

namespace QuickClassMap.Tests;

public sealed class RoslynClassParserFixture
{
    public List<ClassInfo> ParseClasses(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var parser = new RoslynClassParser(
            new Dictionary<INamedTypeSymbol, ClassInfo>(SymbolEqualityComparer.Default));

        return parser.ParseClasses(syntaxTree, semanticModel);
    }
}
