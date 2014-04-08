using PTORMPrototype.Mapping.Configuration;

namespace PTORMPrototype.Query
{
    public class SubTypePart : TypePart
    {
        public TypeMappingInfo CollectingType { get; set; }
        public PropertyMapping CollectingProperty { get; set; }
    }
}