using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PTORMPrototype.Mapping.Configuration
{
    public class FluentConfiguration
    {
        private readonly IList<TypeMappingConfig> _typeConfigs = new List<TypeMappingConfig>();
        private string _defaultIdProperty;
        private string _defaultDiscriminatorColumnName;

        public static FluentConfiguration Start()
        {
            return new FluentConfiguration();
        }

        public string IdPropertyName { get { return _defaultIdProperty; } }
        public string DiscriminatorColumnName { get { return _defaultDiscriminatorColumnName; } }


        public FluentConfiguration DefaultIdProperty(Expression<MemberExpression> propertyExpression)
        {
            if (propertyExpression == null)
                throw new ArgumentNullException("propertyExpression");
            return DefaultIdProperty(((MemberExpression)propertyExpression.Body).Member.Name);
        }

        public FluentConfiguration DefaultIdProperty(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) 
                throw new ArgumentException("name");
            _defaultIdProperty = name;
            return this;
        }

        public FluentConfiguration DefaultDiscriminatorColumnName(string defaultName)
        {
            if (string.IsNullOrWhiteSpace(defaultName))
                throw new ArgumentException("defaultName");
            _defaultDiscriminatorColumnName = defaultName;
            return this;
        }

        public FluentConfiguration AddTypeAuto<T>()
            where T : new()
        {
            return AddType<T>(z => z.AllProperties());
        }

        public FluentConfiguration AddType<T>(Action<TypeMappingConfig> fillType)
            where T : new()
        {
            return AddType(typeof (T), fillType);
        }

        public FluentConfiguration AddTypeAuto(Type type)
        {
            return AddType(type, z => z.AllProperties());
        }

        public FluentConfiguration AddType(Type type, Action<TypeMappingConfig> fillType)
        { 
            if (fillType == null) 
                throw new ArgumentNullException("fillType");
            var info = new TypeMappingConfig(this, type);
            fillType(info);
            _typeConfigs.Add(info);
            return this;
        }

        public IEnumerable<TypeMappingInfo> GenerateTypeMappings()
        {
            var typeMappings = _typeConfigs.Select(z => z.Mapping).ToList();
            var hierarchyDict = typeMappings.ToLookup(z => z.Type.BaseType);
            var typesDict = typeMappings.ToDictionary(z => z.Type);
            // can be paralleled
            foreach (var typeConfig in _typeConfigs)
            {
                var mappingInfo = typeConfig.Mapping;
                var type = mappingInfo.Type;
                var myTable = new TableInfo
                {
                    Name = type.Name,
                    DiscriminatorColumn = _defaultDiscriminatorColumnName,
                    IdentityColumn = mappingInfo.IdentityField ?? _defaultIdProperty
                };
                mappingInfo.Tables.Add(myTable);
            }
            foreach (var typeConfig in _typeConfigs)
            {
                var mappingInfo = typeConfig.Mapping;
                var type = mappingInfo.Type;
                var myTable = mappingInfo.Tables.First();
                foreach (var property in typeConfig.MappedProperties)
                {
                    var propertyType = property.PropertyType;
                    if (propertyType.IsSqlPrimitive())
                    {
                        var propertyMapping = new PropertyMapping
                        {
                            Table = myTable,
                            ColumnName = property.Name,
                            Name = property.Name,
                            DeclaredType = mappingInfo,
                            Nullable = type.IsNullable()
                        };
                        myTable.Columns.Add(propertyMapping);
                    }
                    else
                    {
                        var navPropertyMapping = new NavigationPropertyMapping
                        {                            
                            Name = property.Name,
                            DeclaredType = mappingInfo,
                            TargetType = typesDict[propertyType]
                        };
                        if (propertyType.IsPrimitiveCollection())
                        {
                            throw new NotImplementedException();                            
                        }
                        else if (propertyType.IsCollection())
                        {
                            var childTable = navPropertyMapping.TargetType.Tables.First();
                            navPropertyMapping.Table = childTable;
                            //todo: delay setting names
                            navPropertyMapping.ColumnName = string.Format("{0}_{1}", type.Name, property.Name);
                            navPropertyMapping.Nullable = false;
                            navPropertyMapping.Host = ReferenceHost.Child;
                            childTable.Columns.Add(navPropertyMapping);
                        }
                        else
                        {
                            navPropertyMapping.Table = myTable;
                            navPropertyMapping.ColumnName = property.Name;
                            navPropertyMapping.Nullable = true;
                            navPropertyMapping.Host = ReferenceHost.Parent;
                            myTable.Columns.Add(navPropertyMapping);
                        }
                    }                    
                }
            }            
            var baseObjects = hierarchyDict[typeof (object)];
            Parallel.ForEach(baseObjects, typeMappingInfo =>
            {
                typeMappingInfo.Hierarchy = new HierarchyInfo {BaseType = typeMappingInfo};
                typeMappingInfo.Hierarchy.AddMapping(typeMappingInfo);
                var hierarchyQueue = new Queue<TypeMappingInfo>();
                hierarchyQueue.Enqueue(typeMappingInfo);
                while (hierarchyQueue.Count > 0)
                {
                    var currentNode = hierarchyQueue.Dequeue();
                    foreach (var childMapping in hierarchyDict[currentNode.Type])
                    {
                        childMapping.RefillFromParent(currentNode);
                        currentNode.Hierarchy.AddMapping(childMapping);
                        hierarchyQueue.Enqueue(childMapping);
                    }
                    currentNode.Complete();
                }                
            });
            return typeMappings;
        }
    }
}