using Microsoft.CodeAnalysis;

using QuickClassMap.Core.Domain;

namespace QuickClassMap.Core.Roslyn.Parsing
{
    internal sealed class SymbolRelationship
    {
        public SymbolRelationship(INamedTypeSymbol source, INamedTypeSymbol target, RelationshipType type)
        {
            Source = source;
            Target = target;
            Type = type;
        }

        public INamedTypeSymbol Source { get; }

        public INamedTypeSymbol Target { get; }

        public RelationshipType Type { get; internal set; }
    }
}
