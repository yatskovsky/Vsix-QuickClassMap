using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Basic.Reference.Assemblies;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using QuickClassMap.Core.Domain;
using QuickClassMap.Core.Roslyn;
using QuickClassMap.Core.Roslyn.Parsing;
using QuickClassMap.Core.Roslyn.Traversal;

namespace QuickClassMap.Tests;

public sealed class ClassGraphWalkerFixture
{
    public Task<List<ClassInfo>> ParseWalkDownAsync(
        string source,
        string seedTypeName,
        int maxDepth)
    {
        return ParseWalkDownAsync(source, new[] { seedTypeName }, maxDepth);
    }

    public async Task<List<ClassInfo>> ParseWalkDownAsync(
        string source,
        IReadOnlyCollection<string> seedTypeNames,
        int maxDepth)
    {
        return await WalkAsync(
            source,
            seedTypeNames,
            new ClassGraphTraversalOptions { MaxDepth = maxDepth },
            ClassGraphTraversalDirection.Down);
    }

    public Task<List<ClassInfo>> ParseWalkUpAsync(
        string source,
        string seedTypeName,
        int maxDepth)
    {
        return ParseWalkUpAsync(
            source,
            seedTypeName,
            new ClassGraphTraversalOptions { MaxDepth = maxDepth });
    }

    public async Task<List<ClassInfo>> ParseWalkUpAsync(
        string source,
        string seedTypeName,
        ClassGraphTraversalOptions options)
    {
        return await WalkAsync(
            source,
            new[] { seedTypeName },
            options,
            ClassGraphTraversalDirection.Up);
    }

    private static async Task<List<ClassInfo>> WalkAsync(
        string source,
        IReadOnlyCollection<string> seedTypeNames,
        ClassGraphTraversalOptions options,
        ClassGraphTraversalDirection direction)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references: Net472.References.All,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var sourceTypeLookup = SourceTypeLookup.Create(compilation);
        var seedSymbols = GetSeedSymbols(sourceTypeLookup, seedTypeNames);

        var graphWalker = new ClassGraphWalker(
            compilation,
            direction == ClassGraphTraversalDirection.Up ? sourceTypeLookup : null);
        var traversalResult = await graphWalker.WalkAsync(
            seedSymbols,
            options,
            direction,
            CancellationToken.None);

        return CreateClassInfos(traversalResult);
    }

    private static List<INamedTypeSymbol> GetSeedSymbols(
        SourceTypeLookup sourceTypeLookup,
        IReadOnlyCollection<string> seedTypeNames)
    {
        var seedSymbols = new List<INamedTypeSymbol>();
        foreach (var seedTypeName in seedTypeNames)
        {
            var matches = sourceTypeLookup.Types
                .Where(sourceType =>
                    string.Equals(sourceType.Symbol.Name, seedTypeName, StringComparison.Ordinal) ||
                    string.Equals(sourceType.Symbol.ToDisplayString(), seedTypeName, StringComparison.Ordinal))
                .Select(sourceType => sourceType.Symbol)
                .ToList();

            if (matches.Count == 0)
            {
                throw new InvalidOperationException($"Could not find seed type '{seedTypeName}'.");
            }

            if (matches.Count > 1)
            {
                throw new InvalidOperationException($"Seed type name '{seedTypeName}' is ambiguous.");
            }

            seedSymbols.Add(matches[0]);
        }

        return seedSymbols;
    }

    private static List<ClassInfo> CreateClassInfos(ClassGraphTraversalResult traversalResult)
    {
        var classInfoMap = new Dictionary<INamedTypeSymbol, ClassInfo>(SymbolEqualityComparer.Default);
        var classParser = new RoslynClassParser(classInfoMap);

        foreach (var symbol in traversalResult.Symbols)
        {
            var classSymbol = symbol.OriginalDefinition;
            classInfoMap[classSymbol] = classParser.GenerateClassInfo(
                classSymbol,
                isInterface: classSymbol.TypeKind == TypeKind.Interface);
        }

        foreach (var relationship in traversalResult.Relationships)
        {
            if (!classInfoMap.TryGetValue(relationship.Source.OriginalDefinition, out var classInfo))
            {
                continue;
            }

            classInfo.Relationships.Add(new RelationshipInfo
            {
                RelatedClassName = relationship.Target.ToDisplayString(),
                Type = relationship.Type
            });
        }

        return new List<ClassInfo>(classInfoMap.Values);
    }

}
