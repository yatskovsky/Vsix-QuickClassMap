namespace QuickClassMap.Domain
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
            if (obj is RelationshipInfo other)
            {
                return RelatedClassName == other.RelatedClassName &&
                    Type == other.Type;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (RelatedClassName, Type).GetHashCode();
        }
    }
}
