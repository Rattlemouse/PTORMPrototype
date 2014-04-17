using System.Collections.Generic;
using System.Text;

namespace PTORMPrototype.Query.Sql
{
    internal class SelectSqlBuilder : FilteredSqlBuilder
    {
        private readonly List<string> _selectClauses = new List<string>();
        private readonly string _fromClause;
        private readonly List<string> _joinClauses = new List<string>();

        public SelectSqlBuilder(TableContext context)
        {
            _fromClause = "FROM " + Escape(context.Table.Name) + " AS " + Escape(context.Alias);
        }

        public override string GetSql()
        {
            var builder = new StringBuilder("SELECT ");
            builder.Append(string.Join(", ", _selectClauses));
            builder.Append(" ");
            builder.Append(_fromClause);
            foreach (var joinClause in _joinClauses)
            {
                builder.Append(" ");
                builder.Append(joinClause);
            }
            if (WhereClauses.Count > 0)
            {
                builder.Append(" WHERE ");
                builder.Append(string.Join(" AND ", WhereClauses));
            }
            return builder.ToString();
        }

        public SelectSqlBuilder InnerJoin(TableContext firstContext, TableContext secondContext, string name, string identityField)
        {
            _joinClauses.Add(string.Format("INNER JOIN {0} AS {1} ON {2} = {3}",
                Escape(secondContext.Table.Name),
                Escape(secondContext.Alias),
                Column(firstContext, name),
                Column(secondContext, identityField)
                ));
            return this;
        }

        public SelectSqlBuilder LeftOuterJoin(TableContext firstContext, TableContext secondContext, string name, string identityField)
        {
            _joinClauses.Add(string.Format("LEFT OUTER JOIN {0} AS {1} ON {2} = {3}",
                Escape(secondContext.Table.Name),
                Escape(secondContext.Alias),
                Column(firstContext, name),
                Column(secondContext, identityField)
                ));
            return this;
        }

        public SelectSqlBuilder SelectAll(TableContext context)
        {
            _selectClauses.Add(string.Format("{0}.*", Escape(context.Alias)));
            return this;
        }

    }
}