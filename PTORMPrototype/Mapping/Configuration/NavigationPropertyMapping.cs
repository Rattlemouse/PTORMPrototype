namespace PTORMPrototype.Mapping.Configuration
{
    public class NavigationPropertyMapping : PropertyMapping
    {
        public TypeMappingInfo TargetType { get; set; }
        public ReferenceHost Host { get; set; }
    }
}