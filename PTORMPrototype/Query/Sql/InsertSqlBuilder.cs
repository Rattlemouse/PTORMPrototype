using System;
using System.Collections.Generic;
using PTORMPrototype.Mapping.Configuration;

namespace PTORMPrototype.Query.Sql
{
    internal class InsertSqlBuilder : SqlBuilder
    {
        private readonly string _insertClause;
        private readonly List<string> _insertProps = new List<string>();
        private readonly List<string> _insertParams = new List<string>();

        public InsertSqlBuilder(Table mainTable)
        {
            if (mainTable == null)
                throw new ArgumentNullException("mainTable");
            _insertClause = string.Format("INSERT INTO {0}", Escape(mainTable.Name));
        }

        public override string GetSql()
        {            
            return string.Format("{0} ({1}) VALUES({2})", _insertClause, string.Join(", ", _insertProps), string.Join(", ", _insertParams));
        }

        public string AddInsert(string columnName)
        {
            _insertProps.Add(Escape(columnName));
            string nextParameter = GetNextParameter();
            _insertParams.Add(nextParameter);
            return nextParameter;
        }


    }
}