using System;
using System.Collections.Generic;
using System.Data;

namespace PTORMPrototype.Mapping.Configuration
{
    public class PrimitiveListTable : Table
    {
        private readonly List<PropertyMapping> _columns;
        public bool MaintainOrder { get; private set; }        

        public PrimitiveListTable(PropertyMapping parentIdColumn, SqlType primitiveType, bool maintainOrder = false)
        {
            if (parentIdColumn == null) 
                throw new ArgumentNullException("parentIdColumn");
            if (primitiveType == null) 
                throw new ArgumentNullException("primitiveType");
            _columns = new List<PropertyMapping>(3)
            {
                new PropertyMapping
                {
                    ColumnName = "ParentId",
                    SqlType = parentIdColumn.SqlType,
                    Table = this
                },
                new PropertyMapping
                {
                    ColumnName = "Value",
                    SqlType = primitiveType,
                    Table = this
                }
            };
            if (maintainOrder)
            {
                _columns.Add(new PropertyMapping
                {
                    ColumnName = "Index",
                    SqlType = new SqlType(SqlDbType.BigInt, false),
                    Table = this
                });
            }
            MaintainOrder = maintainOrder;
        }

        public override IList<PropertyMapping> Columns
        {
            get { return _columns; }
        }
    }
}