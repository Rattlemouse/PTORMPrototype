using System.Data.SqlTypes;
using System.Dynamic;

namespace PTORMPrototype.Mapping.Configuration
{
    public class PropertyMapping
    {
        public TypeMappingInfo DeclaredType { get; set; }
        public string Name { get; set; }
        public TableInfo Table { get; set; }
        public string ColumnName { get; set; }
        public bool Nullable { get { return SqlType.Nullable; } }
        public SqlType SqlType { get; set; }
    }
}