using System.Collections.Generic;
using System.Text;

namespace PTORMPrototype.Query.Sql
{
    internal class UpdateSqlBuilder : FilteredSqlBuilder
    {
        private readonly string _updateClause;
        private readonly List<string> _sets = new List<string>();
        private readonly string _fromClause;

        public UpdateSqlBuilder(TableContext mainTableContext)
        {
            string alias = Escape(mainTableContext.Alias);
            _updateClause = string.Format("UPDATE {0} SET", alias);
            _fromClause = string.Format(" FROM {0} AS {1}", Escape(mainTableContext.Table.Name), alias);
        }

        public override string GetSql()
        {
            
            var builder = new StringBuilder(_updateClause);
            builder.Append(" ");
            builder.Append(string.Join(", ", _sets));
            builder.Append(_fromClause);
            if (WhereClauses.Count > 0)
            {
                builder.Append(" WHERE ");
                builder.Append(string.Join(" AND ", WhereClauses));
            }
            return builder.ToString();
        }

        public string Set(TableContext mainTableContext, string column)
        {
            var parm = GetNextParameter();
            _sets.Add(GetEquality(mainTableContext, column, parm));
            return parm;
        }    
    }
}