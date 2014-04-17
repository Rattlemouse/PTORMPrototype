using System.Collections.Generic;

namespace PTORMPrototype.Query
{
    internal class AliasContext
    {
        readonly Dictionary<string, string> _aliases = new Dictionary<string, string>();
        private readonly string _context;

        public AliasContext(string context)
        {
            _context = context;
        }

        public string GetTableAlias(string tableName)
        {
            string alias;
            if (_aliases.TryGetValue(tableName, out alias))
                return alias;
            alias = string.Format("{1}{0}", _aliases.Count + 1, _context);
            _aliases.Add(tableName, alias);
            return alias;
        }
    }
}