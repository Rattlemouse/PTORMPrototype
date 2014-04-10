using System.Collections.Generic;
using PTORMPrototype.Mapping.Configuration;

namespace PTORMPrototype.Query
{
    public class TypePart : SelectPart
    {
        public IList<Table> Tables { get; set; }
        public TypeMappingInfo Type { get; set; }
    }
}