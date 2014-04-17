using System.Text;

namespace PTORMPrototype.Query.Sql
{
    internal class DeleteSqlBuilder : FilteredSqlBuilder
    {
        private readonly string _deleteClause;        

        public DeleteSqlBuilder(TableContext mainTableContext)
        {
            _deleteClause = string.Format("DELETE FROM {0} AS {1}", Escape(mainTableContext.Table.Name), Escape(mainTableContext.Alias));
        }
        public override string GetSql()
        {

            var builder = new StringBuilder(_deleteClause);                       
            if (WhereClauses.Count > 0)
            {
                builder.Append(" WHERE ");
                builder.Append(string.Join(" AND ", WhereClauses));
            }
            return builder.ToString();
        }
    }
}