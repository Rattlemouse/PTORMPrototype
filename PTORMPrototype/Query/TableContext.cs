using PTORMPrototype.Mapping.Configuration;

namespace PTORMPrototype.Query
{
    internal class TableContext
    {
        private readonly Table _table;
        private readonly string _alias;

        public TableContext(Table table, string alias)
        {
            _table = table;
            _alias = alias;
        }

        public Table Table
        {
            get { return _table; }
        }

        public string Alias
        {
            get { return _alias; }
        }
    }
}