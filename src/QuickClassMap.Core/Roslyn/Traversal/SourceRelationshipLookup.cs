using System;
using System.Collections.Generic;
using System.Threading;

using Microsoft.CodeAnalysis;

using QuickClassMap.Core.Roslyn.Parsing;

namespace QuickClassMap.Core.Roslyn.Traversal
{
    internal sealed class SourceRelationshipLookup
    {
        private readonly RoslynRelationshipParser _relationshipParser;
        private readonly SourceTypeLookup _sourceTypeLookup;
        private readonly Dictionary<INamedTypeSymbol, IReadOnlyCollection<SymbolRelationship>> _relationshipsBySource;

        private Dictionary<INamedTypeSymbol, List<SymbolRelationship>> _relationshipsByTarget;

        public SourceRelationshipLookup(
            RoslynRelationshipParser relationshipParser,
            SourceTypeLookup sourceTypeLookup = null)
        {
            _relationshipParser = relationshipParser ?? throw new ArgumentNullException(nameof(relationshipParser));
            _sourceTypeLookup = sourceTypeLookup;
            _relationshipsBySource = new Dictionary<INamedTypeSymbol, IReadOnlyCollection<SymbolRelationship>>(
                SymbolEqualityComparer.Default);
        }

        public IReadOnlyCollection<SymbolRelationship> GetRelationships(INamedTypeSymbol source)
        {
            source = source.OriginalDefinition;
            if (!_relationshipsBySource.TryGetValue(source, out var relationships))
            {
                relationships = _relationshipParser.ExtractSymbolRelationships(source);
                _relationshipsBySource.Add(source, relationships);
            }

            return relationships;
        }

        public IReadOnlyCollection<SymbolRelationship> GetDependents(
            INamedTypeSymbol target,
            CancellationToken cancellationToken)
        {
            EnsureIncomingRelationships(cancellationToken);
            target = target.OriginalDefinition;
            return _relationshipsByTarget.TryGetValue(target, out var relationships)
                ? relationships
                : Array.Empty<SymbolRelationship>();
        }

        private void EnsureIncomingRelationships(CancellationToken cancellationToken)
        {
            if (_relationshipsByTarget != null)
            {
                return;
            }

            if (_sourceTypeLookup == null)
            {
                throw new InvalidOperationException("A source type lookup is required to find dependents.");
            }

            var relationshipsByTarget = new Dictionary<INamedTypeSymbol, List<SymbolRelationship>>(
                SymbolEqualityComparer.Default);
            foreach (var sourceType in _sourceTypeLookup.Types)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var relationship in GetRelationships(sourceType.Symbol))
                {
                    var target = relationship.Target.OriginalDefinition;
                    if (!relationshipsByTarget.TryGetValue(target, out var relationships))
                    {
                        relationships = new List<SymbolRelationship>();
                        relationshipsByTarget.Add(target, relationships);
                    }

                    relationships.Add(relationship);
                }
            }

            _relationshipsByTarget = relationshipsByTarget;
        }
    }
}
