namespace PTORMPrototype.Query.Sql
{
    internal abstract class SqlBuilder
    {
        private int _currentParameter = 0;

        

        public abstract string GetSql();

        protected static string Escape(string name)
        {
            return "[" + name + "]";
        }

        protected static string Column(TableContext table, string column)
        {
            return string.Format("{0}.{1}", Escape(table.Alias), Escape(column));
        }

        public string GetNextParameter()
        {
            return GetParameter(GetNextParameteIndex());
        }

        protected int GetNextParameteIndex()
        {
            return _currentParameter++;
        }

        public string GetParameter(int parameterIndex)
        {
            return "@p" + parameterIndex;
        }

        public string GetEquality(TableContext table, string column, TableContext table2, string column2)
        {
            return GetEquality(table, column, Column(table2, column2));
        }

        public string GetEquality(TableContext table, string column, string operand)
        {
            return string.Format("{0} = {1}", Column(table, column), operand);
        }
        
    }
}