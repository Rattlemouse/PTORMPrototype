using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Linq.Expressions;

namespace PTORMPrototype.Mapping.Configuration
{
    public class TypeMappingInfo
    {
        private readonly PropertyMappingCollection _propertyMappings = new PropertyMappingCollection();
        private readonly List<Table> _tables = new List<Table>();
        private readonly ICollection<TypeMappingInfo> _children = new Collection<TypeMappingInfo>();

        public HierarchyInfo Hierarchy { get; set; }

        public ICollection<Table> Tables
        {
            get { return _tables; }            
        }

        public Type Type { get; set; }
        public string IdentityField { get; set; }
        public int Discriminator { get; set; }

        public ICollection<TypeMappingInfo> Children
        {
            get { return _children; }            
        }

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


        public bool HasProperty(string fieldName)
        {
            return _propertyMappings.Contains(fieldName);
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

        public IEnumerable<TypeMappingInfo> GetNearestChildrenThatHaveProperty(string propertyName)
        {
            var childrenToVisit = new Queue<TypeMappingInfo>(Children);
            while (childrenToVisit.Count > 0)
            {
                var typeMapping = childrenToVisit.Dequeue();                
                if (typeMapping.HasProperty(propertyName))
                {
                    yield return typeMapping;                        
                }
                else
                {
                    foreach (var c in typeMapping.Children)                        
                        childrenToVisit.Enqueue(c);
                }                                
            }
        }
    }
}