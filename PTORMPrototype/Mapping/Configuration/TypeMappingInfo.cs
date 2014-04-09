using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;

namespace PTORMPrototype.Mapping.Configuration
{
    public class TypeMappingInfo
    {
        public HierarchyInfo Hierarchy { get; set; }

        public ICollection<TableInfo> Tables
        {
            get { return _tables; }            
        }

        public Type Type { get; set; }
        public string IdentityField { get; set; }
        public int Discriminator { get; set; }

        private readonly PropertyMappingCollection _navigationPropertyMappings = new PropertyMappingCollection();
        private readonly PropertyMappingCollection _propertyMappings = new PropertyMappingCollection();
        private readonly List<TableInfo> _tables = new List<TableInfo>();

        //public IEnumerable<PropertyMapping> PropertyMappings { get { return _propertyMappings; } }
        //public IEnumerable<NavigationPropertyMapping> NavigationPropertyMappings { get { return _navigationPropertyMappings.OfType<NavigationPropertyMapping>(); } }

        public void RefillFromParent(TypeMappingInfo parentMapping)
        {                       
            _tables.InsertRange(0, parentMapping.Tables);            
        }

        public NavigationPropertyMapping GetNavigation(string fieldName)
        {
            return (NavigationPropertyMapping) _navigationPropertyMappings[fieldName];
        }

        public PropertyMapping GetProperty(string fieldName)
        {
            return _propertyMappings[fieldName];
        }

        public void Complete()
        {
            foreach (var column in _tables.SelectMany(z => z.Columns))
            {
                var nav = column as NavigationPropertyMapping;
                if (nav != null)
                {
                    if(nav.Host == ReferenceHost.Child && nav.DeclaredType != this)
                        continue;
                    _navigationPropertyMappings.Add(column);
                }
                else
                    _propertyMappings.Add(column);
            }            
        }
    }
}