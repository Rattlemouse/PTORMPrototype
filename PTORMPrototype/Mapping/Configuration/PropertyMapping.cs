namespace PTORMPrototype.Mapping.Configuration
{
    public class PropertyMapping
    {
        public TypeMappingInfo DeclaredType { get; set; }
        public string Name { get; set; }
        public TableInfo Table { get; set; }
        public string ColumnName { get; set; }
        public bool Nullable { get; set; }
    }
}