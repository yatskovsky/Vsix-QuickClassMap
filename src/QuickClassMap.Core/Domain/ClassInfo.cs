using System.Collections.Generic;

namespace QuickClassMap.Core.Domain
{
    public class ClassInfo
    {
        public string FullName { get; set; }
        public string Name { get; set; }
        public string SourceFilePath { get; set; }
        public HashSet<RelationshipInfo> Relationships { get; set; } = new HashSet<RelationshipInfo>();
        public bool IsInterface { get; set; }
    }
}
