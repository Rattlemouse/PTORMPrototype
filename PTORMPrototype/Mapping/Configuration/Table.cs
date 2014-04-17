using System.Collections.Generic;
using System.Data.SqlClient;

namespace PTORMPrototype.Mapping.Configuration
{
    public abstract class Table
    {
        public string Name { get; set; }
        public abstract IList<PropertyMapping> Columns { get; }
    }
}