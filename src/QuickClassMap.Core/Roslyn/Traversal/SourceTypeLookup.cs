using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;

using QuickClassMap.Core.Domain;
using QuickClassMap.Core.Roslyn.Parsing;

namespace QuickClassMap.Core.Roslyn.Traversal
{
    internal sealed class SourceTypeLookup
    {
        private readonly Dictionary<INamedTypeSymbol, SourceTypeInfo> _typesBySymbol;

        private SourceTypeLookup()
        {
            _typesBySymbol = new Dictionary<INamedTypeSymbol, SourceTypeInfo>(SymbolEqualityComparer.Default);
        }

        public IReadOnlyCollection<SourceTypeInfo> Types => _typesBySymbol.Values.ToList();

        public static SourceTypeLookup Create(Compilation compilation)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            var lookup = new SourceTypeLookup();
            var classParser = new RoslynClassParser(
                new Dictionary<INamedTypeSymbol, ClassInfo>(SymbolEqualityComparer.Default));

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                foreach (var sourceType in classParser.DiscoverSourceTypes(syntaxTree, semanticModel))
                {
                    lookup.Add(sourceType);
                }
            }

            return lookup;
        }

        public IReadOnlyCollection<INamedTypeSymbol> GetSymbols(string filePath)
        {
            if (filePath == null)
            {
                return Array.Empty<INamedTypeSymbol>();
            }

            var normalizedFilePath = NormalizeFilePath(filePath);
            return _typesBySymbol.Values
                .Where(sourceType => sourceType.FilePaths.Any(path =>
                    string.Equals(NormalizeFilePath(path), normalizedFilePath, StringComparison.OrdinalIgnoreCase)))
                .Select(sourceType => sourceType.Symbol)
                .ToList();
        }

        public bool Contains(INamedTypeSymbol symbol)
        {
            return symbol != null && _typesBySymbol.ContainsKey(symbol.OriginalDefinition);
        }

        private void Add(SourceTypeInfo sourceType)
        {
            var symbol = sourceType.Symbol.OriginalDefinition;
            if (_typesBySymbol.TryGetValue(symbol, out var existingSourceType))
            {
                sourceType = new SourceTypeInfo(
                    symbol,
                    existingSourceType.Declarations.Concat(sourceType.Declarations));
            }

            _typesBySymbol[symbol] = sourceType;
        }

        private static string NormalizeFilePath(string filePath)
        {
            return Path.GetFullPath(filePath);
        }
    }
}
