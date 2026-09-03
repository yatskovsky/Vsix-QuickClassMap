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

            DeepRelationshipTypes = new HashSet<RelationshipType>(RelationshipTypes);
            DeepRelationshipTypes.Remove(RelationshipType.Uses);
        }

        public int MaxDepth { get; init; } = 1;

        public ISet<RelationshipType> GetRelationshipTypes(int currentDepth)
        {
            if (currentDepth <= 1)
            {
                return RelationshipTypes;
            }

            return DeepRelationshipTypes;
        }

        private ISet<RelationshipType> RelationshipTypes { get; }

        private ISet<RelationshipType> DeepRelationshipTypes { get; }
    }
}
