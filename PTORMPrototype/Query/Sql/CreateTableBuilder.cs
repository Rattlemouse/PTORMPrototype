using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PTORMPrototype.Mapping.Configuration;

namespace PTORMPrototype.Query.Sql
{
    internal class CreateTableBuilder : SqlBuilder
    {
        private readonly TypeMappingInfo _mappingInfo;

        public CreateTableBuilder(TypeMappingInfo mappingInfo)
        {            
            if (mappingInfo == null) 
                throw new ArgumentNullException("mappingInfo");
            _mappingInfo = mappingInfo;
        }

        public override string GetSql()
        {            
            var table = _mappingInfo.Tables.OfType<EntityTable>().Last();
            return string.Join("\n", new[] { CreateTable(table) }.Concat(_mappingInfo.Tables.OfType<PrimitiveListTable>().Select(CreateTable)));
        }

        private static string CreateTable(Table table)
        {
            var stringBuilder = new StringBuilder("CREATE TABLE ");
            stringBuilder.AppendFormat("[{0}]", table.Name);
            stringBuilder.Append(" (");
            var columns = new List<string>(table.Columns.Count + 2);
            var entityTabe = table as EntityTable;
            if (entityTabe != null)
            {
                columns.Add(string.Format("[{0}] {1} PRIMARY KEY", entityTabe.IdentityColumn.ColumnName,
                    entityTabe.IdentityColumn.SqlType.ToString().ToUpper()));
                if (entityTabe.HasDiscriminator)
                {
                    columns.Add(string.Format("[{0}] INT NOT NULL", entityTabe.DiscriminatorColumn));
                }
            }
            columns.AddRange(table.Columns.Select(column => string.Format("[{0}] {1}", column.ColumnName, column.SqlType.ToString().ToUpper())));
            stringBuilder.Append(string.Join(", ", columns));
            stringBuilder.Append(");");
            return stringBuilder.ToString();
        }
    }
}