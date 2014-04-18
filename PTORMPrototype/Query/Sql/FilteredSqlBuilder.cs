using System.Collections.Generic;
using System.Linq;

namespace PTORMPrototype.Query.Sql
{
    internal abstract class FilteredSqlBuilder : SqlBuilder
    {
        protected readonly List<string> WhereClauses = new List<string>();

        public FilteredSqlBuilder Where(string getEqualPredicate)
        {
            WhereClauses.Add(getEqualPredicate);
            return this;
        }


        public string Or(ICollection<string> predicates)
        {
            if (predicates.Count == 1)
                return predicates.First();
            return string.Format("({0})", string.Join(" OR ", predicates));
        }
    }
}