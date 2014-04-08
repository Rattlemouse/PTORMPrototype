using PTORMPrototype.Mapping.Configuration;

namespace PTORMPrototype.Query
{
    public class ColumnCollectionPart : SelectPart
    {
        public PropertyMapping[] Columns { get; set; }
    }
}