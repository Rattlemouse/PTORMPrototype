using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;

namespace PTORMPrototype.Mapping.Configuration
{
    public class TypeMappingInfo
    {
        public HierarchyInfo Hierarchy { get; set; }

        public ICollection<Table> Tables
        {
            get { return _tables; }            
        }

        public Type Type { get; set; }
        public string IdentityField { get; set; }
        public int Discriminator { get; set; }

        private readonly PropertyMappingCollection _propertyMappings = new PropertyMappingCollection();
        private readonly List<Table> _tables = new List<Table>();

        //public IEnumerable<PropertyMapping> PropertyMappings { get { return _propertyMappings; } }
        //public IEnumerable<NavigationPropertyMapping> NavigationPropertyMappings { get { return _navigationPropertyMappings.OfType<NavigationPropertyMapping>(); } }

        public void RefillFromParent(TypeMappingInfo parentMapping)
        {                       
            _tables.InsertRange(0, parentMapping.Tables.OfType<EntityTable>());            
            for (var i = 0; i < parentMapping._propertyMappings.Count; i++)
                _propertyMappings.Insert(i, parentMapping._propertyMappings[i]);

        }

        public PropertyMapping GetProperty(string fieldName)
        {
            return _propertyMappings[fieldName];
        }       

        public void AddProperty(PropertyMapping propertyMapping)
        {            
            _propertyMappings.Add(propertyMapping);
        }

        public IEnumerable<NavigationPropertyMapping> GetNavigationProperties()
        {
            return _propertyMappings.OfType<NavigationPropertyMapping>();
        }

        public IEnumerable<PropertyMapping> GetProperties()
        {
            return _propertyMappings;
        }
    }
}