using System.Collections.ObjectModel;

namespace PTORMPrototype.Mapping.Configuration
{
    public class PropertyMappingCollection : KeyedCollection<string, PropertyMapping>
    {
        protected override string GetKeyForItem(PropertyMapping item)
        {
            return item.Name;
        }
    }
}