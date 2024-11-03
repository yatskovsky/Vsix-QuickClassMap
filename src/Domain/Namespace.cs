using System.Collections.Generic;
using System.Linq;

namespace QuickClassMap.Domain
{
    public class Namespace
    {
        private readonly string[] _parts;

        public Namespace(string fullName)
        {
            _parts = string.IsNullOrEmpty(fullName)
                ? new string[0]
                : fullName.Split('.');
        }

        public Namespace(IEnumerable<string> parts)
        {
            _parts = parts.ToArray();
        }

        public ICollection<string> Parts => _parts;

        public string Name => _parts.Length > 0 ? _parts[_parts.Length - 1] : string.Empty;

        public Namespace Parent =>
            _parts.Length > 1 ? new Namespace(_parts.Take(_parts.Length - 1)) : null;

        public bool IsRoot => _parts.Length == 0;

        public string FullName => string.Join(".", _parts);

        public string FullPrefix => FullName + ".";


        public Namespace Append(string part)
        {
            return new Namespace(_parts.Concat(new[] { part }));
        }

        public string StripNamespace(string fullName)
        {
            return fullName.StartsWith(FullPrefix)
                ? fullName.Substring(FullPrefix.Length)
                : fullName;
        }

        public override string ToString() => FullName;

        public override bool Equals(object obj)
        {
            return obj is Namespace other &&
                   FullName.Equals(other.FullName);
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }
    }
}
