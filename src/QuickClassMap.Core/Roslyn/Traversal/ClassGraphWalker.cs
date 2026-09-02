using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

using QuickClassMap.Core.Domain;
using QuickClassMap.Core.Roslyn.Parsing;

namespace QuickClassMap.Core.Roslyn.Traversal
{
    internal sealed class ClassGraphWalker
    {
        private readonly Compilation _compilation;
        private readonly SourceTypeLookup _sourceTypeLookup;

        public ClassGraphWalker(Compilation compilation, SourceTypeLookup sourceTypeLookup = null)
        {
            _compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
            _sourceTypeLookup = sourceTypeLookup;
        }

        public async Task<ClassGraphTraversalResult> WalkDownAsync(
            IReadOnlyCollection<INamedTypeSymbol> seedSymbols,
            ClassGraphTraversalOptions options,
            CancellationToken cancellationToken)
        {
            return await WalkAsync(
                seedSymbols,
                options,
                ClassGraphTraversalDirection.Down,
                cancellationToken);
        }

        public async Task<ClassGraphTraversalResult> WalkAsync(
            IReadOnlyCollection<INamedTypeSymbol> seedSymbols,
            ClassGraphTraversalOptions options,
            ClassGraphTraversalDirection direction,
            CancellationToken cancellationToken)
        {
            if (seedSymbols == null)
            {
                throw new ArgumentNullException(nameof(seedSymbols));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.MaxDepth < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options.MaxDepth), "Maximum traversal depth cannot be negative.");
            }

            if (direction == ClassGraphTraversalDirection.Up && _sourceTypeLookup == null)
            {
                throw new InvalidOperationException("A source type lookup is required for Walk Up.");
            }

            var relationshipParser = new RoslynRelationshipParser(
                _compilation,
                new Dictionary<INamedTypeSymbol, ClassInfo>(SymbolEqualityComparer.Default));
            var discoveredSymbols = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var scheduledSymbols = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var relationshipCache = new Dictionary<INamedTypeSymbol, IReadOnlyCollection<SymbolRelationship>>(
                SymbolEqualityComparer.Default);
            var discoveredRelationships = new List<SymbolRelationship>();
            var pendingSymbols = new Queue<SymbolDepth>();

            foreach (var seedSymbol in seedSymbols)
            {
                var symbol = seedSymbol.OriginalDefinition;
                if (discoveredSymbols.Add(symbol))
                {
                    EnqueueSymbol(symbol, 0, scheduledSymbols, pendingSymbols);
                }
            }

            while (pendingSymbols.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var current = pendingSymbols.Dequeue();
                if (direction == ClassGraphTraversalDirection.Down)
                {
                    foreach (var relationship in GetRelationships(current.Symbol, relationshipParser, relationshipCache))
                    {
                        if (!options.RelationshipTypes.Contains(relationship.Type))
                        {
                            continue;
                        }

                        var target = relationship.Target.OriginalDefinition;
                        if (direction == ClassGraphTraversalDirection.Down
                            ? !IsSourceSymbol(target)
                            : !_sourceTypeLookup.Contains(target))
                        {
                            continue;
                        }

                        if (!TryDiscoverSymbol(
                            target,
                            current.Depth,
                            options.MaxDepth,
                            discoveredSymbols))
                        {
                            continue;
                        }

                        AddRelationship(discoveredRelationships, relationship);
                        EnqueueSymbol(target, current.Depth + 1, scheduledSymbols, pendingSymbols);
                    }
                }
                else if (direction == ClassGraphTraversalDirection.Up)
                {
                    foreach (var sourceType in _sourceTypeLookup.Types)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        foreach (var relationship in GetRelationships(sourceType.Symbol, relationshipParser, relationshipCache))
                        {
                            if (!options.RelationshipTypes.Contains(relationship.Type) ||
                                !SymbolEqualityComparer.Default.Equals(relationship.Target, current.Symbol))
                            {
                                continue;
                            }

                            var source = relationship.Source.OriginalDefinition;
                            if (!TryDiscoverSymbol(
                                source,
                                current.Depth,
                                options.MaxDepth,
                                discoveredSymbols))
                            {
                                continue;
                            }

                            AddRelationship(discoveredRelationships, relationship);
                            EnqueueSymbol(source, current.Depth + 1, scheduledSymbols, pendingSymbols);
                        }
                    }
                }
            }

            return new ClassGraphTraversalResult(
                discoveredSymbols.ToList(),
                discoveredRelationships);
        }

        private bool IsSourceSymbol(INamedTypeSymbol symbol)
        {
            return symbol != null &&
                (symbol.TypeKind == TypeKind.Class || symbol.TypeKind == TypeKind.Interface) &&
                symbol.Locations.Any(location =>
                    location.IsInSource &&
                    location.SourceTree != null &&
                    _compilation.ContainsSyntaxTree(location.SourceTree));
        }

        private IReadOnlyCollection<SymbolRelationship> GetRelationships(
            INamedTypeSymbol source,
            RoslynRelationshipParser relationshipParser,
            IDictionary<INamedTypeSymbol, IReadOnlyCollection<SymbolRelationship>> relationshipCache)
        {
            source = source.OriginalDefinition;
            if (!relationshipCache.TryGetValue(source, out var relationships))
            {
                relationships = relationshipParser.ExtractSymbolRelationships(source);
                relationshipCache.Add(source, relationships);
            }

            return relationships;
        }

        private bool TryDiscoverSymbol(
            INamedTypeSymbol symbol,
            int currentDepth,
            int maxDepth,
            ISet<INamedTypeSymbol> discoveredSymbols)
        {
            if (discoveredSymbols.Contains(symbol))
            {
                return true;
            }

            if (currentDepth >= maxDepth)
            {
                return false;
            }

            return discoveredSymbols.Add(symbol);
        }

        private void EnqueueSymbol(
            INamedTypeSymbol symbol,
            int depth,
            ISet<INamedTypeSymbol> scheduledSymbols,
            Queue<SymbolDepth> pendingSymbols)
        {
            symbol = symbol.OriginalDefinition;
            if (scheduledSymbols.Add(symbol))
            {
                pendingSymbols.Enqueue(new SymbolDepth(symbol, depth));
            }
        }

        private void AddRelationship(ICollection<SymbolRelationship> relationships, SymbolRelationship relationship)
        {
            var existingRelationship = relationships.FirstOrDefault(existing =>
                SymbolEqualityComparer.Default.Equals(existing.Source, relationship.Source) &&
                SymbolEqualityComparer.Default.Equals(existing.Target, relationship.Target));

            if (existingRelationship == null)
            {
                relationships.Add(relationship);
            }
            else if (relationship.Type < existingRelationship.Type)
            {
                existingRelationship.Type = relationship.Type;
            }
        }

        private sealed class SymbolDepth
        {
            public SymbolDepth(INamedTypeSymbol symbol, int depth)
            {
                Symbol = symbol;
                Depth = depth;
            }

            public INamedTypeSymbol Symbol { get; }

            public int Depth { get; }
        }
    }
}
