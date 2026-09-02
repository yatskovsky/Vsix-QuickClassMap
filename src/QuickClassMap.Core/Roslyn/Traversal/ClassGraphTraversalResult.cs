using System.Collections.Generic;

using Microsoft.CodeAnalysis;

using QuickClassMap.Core.Roslyn.Parsing;

namespace QuickClassMap.Core.Roslyn.Traversal
{
    internal sealed class ClassGraphTraversalResult
    {
        public ClassGraphTraversalResult(
            IReadOnlyCollection<INamedTypeSymbol> symbols,
            IReadOnlyCollection<SymbolRelationship> relationships)
        {
            Symbols = symbols;
            Relationships = relationships;
        }

        public IReadOnlyCollection<INamedTypeSymbol> Symbols { get; }

        public IReadOnlyCollection<SymbolRelationship> Relationships { get; }
    }
}
