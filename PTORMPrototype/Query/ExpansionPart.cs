using PTORMPrototype.Mapping.Configuration;

namespace PTORMPrototype.Query
{
    public class ExpansionPart : SelectPart
    {
        public TypeMappingInfo CollectingType { get; set; }
        public PropertyMapping CollectingProperty { get; set; }
        public Table Table { get; set; }
    }
}