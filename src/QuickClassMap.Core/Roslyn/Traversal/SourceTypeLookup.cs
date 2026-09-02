using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;

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

        public IEnumerable<SourceTypeInfo> Types => _typesBySymbol.Values;

        public static SourceTypeLookup Create(Compilation compilation)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            var lookup = new SourceTypeLookup();
            foreach (var symbol in EnumerateSourceTypes(compilation.Assembly.GlobalNamespace))
            {
                lookup.Add(new SourceTypeInfo(symbol, symbol.DeclaringSyntaxReferences));
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

        private static IEnumerable<INamedTypeSymbol> EnumerateSourceTypes(INamespaceSymbol namespaceSymbol)
        {
            foreach (var typeSymbol in namespaceSymbol.GetTypeMembers())
            {
                foreach (var nestedType in EnumerateSourceTypes(typeSymbol))
                {
                    yield return nestedType;
                }
            }

            foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                foreach (var typeSymbol in EnumerateSourceTypes(nestedNamespace))
                {
                    yield return typeSymbol;
                }
            }
        }

        private static IEnumerable<INamedTypeSymbol> EnumerateSourceTypes(INamedTypeSymbol typeSymbol)
        {
            if (typeSymbol.TypeKind == TypeKind.Class || typeSymbol.TypeKind == TypeKind.Interface)
            {
                yield return typeSymbol;
            }

            foreach (var nestedType in typeSymbol.GetTypeMembers())
            {
                foreach (var sourceType in EnumerateSourceTypes(nestedType))
                {
                    yield return sourceType;
                }
            }
        }

        private static string NormalizeFilePath(string filePath)
        {
            return Path.GetFullPath(filePath);
        }
    }
}
