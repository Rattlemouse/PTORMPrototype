using System.Collections.Generic;
using System.Text;

namespace PTORMPrototype.Query.Sql
{
    internal class UpdateSqlBuilder : FilteredSqlBuilder
    {
        private readonly string _updateClause;
        private readonly List<string> _sets = new List<string>();

        public UpdateSqlBuilder(TableContext mainTableContext)
        {
            _updateClause = string.Format("UPDATE {0} AS {1} SET", Escape(mainTableContext.Table.Name), Escape(mainTableContext.Alias));
        }
        public override string GetSql()
        {
            
            var builder = new StringBuilder(_updateClause);
            builder.Append(" ");
            builder.Append(string.Join(", ", _sets));
            if (WhereClauses.Count > 0)
            {
                builder.Append(" WHERE ");
                builder.Append(string.Join(" AND ", WhereClauses));
            }
            return builder.ToString();
        }

        public void Set(TableContext mainTableContext, string column)
        {
            _sets.Add(GetEquality(mainTableContext, column));                
        }    
    }
}