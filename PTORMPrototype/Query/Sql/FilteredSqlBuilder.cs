using System.Collections.Generic;

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
    }
}