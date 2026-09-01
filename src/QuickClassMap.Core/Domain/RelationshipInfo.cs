namespace QuickClassMap.Core.Domain
{
    public enum RelationshipType
    {
        Inherits,
        Implements,
        Composes,  // Strong ownership, part cannot exist without the whole
        Aggregates, // Weak ownership, part can exist independently
        Uses        // General usage

    }

    public class RelationshipInfo
    {
        public string RelatedClassName { get; set; }
        public RelationshipType Type { get; set; }

        public override bool Equals(object obj)
        {
            return obj is RelationshipInfo other &&
                RelatedClassName == other.RelatedClassName;
        }

        public override int GetHashCode()
        {
            return RelatedClassName.GetHashCode();
        }
    }
}
