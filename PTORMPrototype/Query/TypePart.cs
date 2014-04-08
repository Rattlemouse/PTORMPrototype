using System.Collections.Generic;
using PTORMPrototype.Mapping.Configuration;

namespace PTORMPrototype.Query
{
    public class TypePart : SelectPart
    {
        public IList<TableInfo> Tables { get; set; }
        public TypeMappingInfo Type { get; set; }
    }
}