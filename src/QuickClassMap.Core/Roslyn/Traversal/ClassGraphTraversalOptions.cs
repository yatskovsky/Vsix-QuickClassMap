using System;
using System.Collections.Generic;

using QuickClassMap.Core.Domain;

namespace QuickClassMap.Core.Roslyn.Traversal
{
    public sealed class ClassGraphTraversalOptions
    {
        public ClassGraphTraversalOptions()
            : this(
            [
                RelationshipType.Inherits,
                RelationshipType.Implements,
                RelationshipType.Composes,
                RelationshipType.Aggregates,
                RelationshipType.Uses
            ])
        {
        }

        public ClassGraphTraversalOptions(IEnumerable<RelationshipType> relationshipTypes)
        {
            if (relationshipTypes == null)
            {
                throw new ArgumentNullException(nameof(relationshipTypes));
            }

            RelationshipTypes = new HashSet<RelationshipType>(relationshipTypes);
        }

        public int MaxDepth { get; init; } = 1;

        public ISet<RelationshipType> RelationshipTypes { get; }
    }
}
