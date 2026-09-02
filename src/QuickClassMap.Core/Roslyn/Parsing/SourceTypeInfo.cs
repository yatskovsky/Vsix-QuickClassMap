using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace QuickClassMap.Core.Roslyn.Parsing
{
    internal sealed class SourceTypeInfo
    {
        public SourceTypeInfo(INamedTypeSymbol symbol, IEnumerable<SyntaxReference> declarations)
        {
            Symbol = symbol.OriginalDefinition;
            Declarations = declarations.ToList();
            FilePaths = Declarations
                .Where(declaration => declaration.SyntaxTree != null)
                .Select(declaration => declaration.SyntaxTree.FilePath)
                .Where(filePath => !string.IsNullOrEmpty(filePath))
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public INamedTypeSymbol Symbol { get; }

        public IReadOnlyCollection<SyntaxReference> Declarations { get; }

        public IReadOnlyCollection<string> FilePaths { get; }
    }
}
