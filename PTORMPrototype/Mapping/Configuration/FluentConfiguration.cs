using System;
using System.Collections.Generic;
using System.Data;
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
                var myTable = new EntityTable
                {
                    Name = type.Name,                    
                    IdentityColumn = new PropertyMapping
                    {
                        ColumnName = mappingInfo.IdentityField ?? _defaultIdProperty,
                        DeclaredType = mappingInfo,
                        Name = mappingInfo.IdentityField,
                        SqlType = GetSqlType(type.GetProperty(mappingInfo.IdentityField).PropertyType)
                    } 
                };
                myTable.IdentityColumn.Table = myTable;
                if (type.BaseType == typeof (object))
                {
                    myTable.DiscriminatorColumn = _defaultDiscriminatorColumnName;
                }
                mappingInfo.Tables.Add(myTable);
            }
            foreach (var typeConfig in _typeConfigs)
            {
                var mappingInfo = typeConfig.Mapping;
                var type = mappingInfo.Type;
                var myTable = (EntityTable) mappingInfo.Tables.First();
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
                            SqlType = GetSqlType(propertyType)
                        };
                        myTable.Columns.Add(propertyMapping);
                        mappingInfo.AddProperty(propertyMapping);
                    }
                    else
                    {
                        var navPropertyMapping = new NavigationPropertyMapping
                        {                            
                            Name = property.Name,
                            DeclaredType = mappingInfo                            
                        };
                        if (propertyType.IsPrimitiveCollection())
                        {                                                        
                            propertyType = propertyType.GetCollectionType();
                            navPropertyMapping.Host = ReferenceHost.Child;  
                            //todo: make options
                            var primitiveListTable = new PrimitiveListTable(myTable.IdentityColumn, GetSqlType(propertyType), true)
                            {
                                Name = string.Format("{0}_{1}", myTable.Name, navPropertyMapping.Name)
                            };
                            navPropertyMapping.Table = primitiveListTable;
                          
                            mappingInfo.Tables.Add(primitiveListTable);
                        }
                        else if (propertyType.IsCollection())
                        {
                            propertyType = propertyType.GetCollectionType();
                            var idType = type.GetProperty(mappingInfo.IdentityField).PropertyType;
                            navPropertyMapping.TargetType = typesDict[propertyType];
                            var childTable = navPropertyMapping.TargetType.Tables.First();                            
                            navPropertyMapping.Table = childTable;
                            //todo: delay setting names
                            navPropertyMapping.ColumnName = string.Format("{0}_{1}", type.Name, property.Name);
                            navPropertyMapping.Host = ReferenceHost.Child;
                            navPropertyMapping.SqlType = GetSqlType(idType);
                            childTable.Columns.Add(navPropertyMapping);
                        }
                        else
                        {
                            navPropertyMapping.TargetType = typesDict[propertyType];
                            var targetClrType = navPropertyMapping.TargetType.Type;
                            var idType = targetClrType.GetProperty(navPropertyMapping.TargetType.IdentityField).PropertyType;                            
                            navPropertyMapping.SqlType = new SqlType(idType.GetSqlType(), true);
                            navPropertyMapping.Table = myTable;
                            navPropertyMapping.ColumnName = property.Name;                            
                            navPropertyMapping.Host = ReferenceHost.Parent;      
                            myTable.Columns.Add(navPropertyMapping);                       
                        }
                        mappingInfo.AddProperty(navPropertyMapping);
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
                }
            });
            return typeMappings;
        }

        private SqlType GetSqlType(Type propertyType)
        {
            var nullable = propertyType.TryExtractFromNullable(out propertyType);
            var sqlType = propertyType.GetSqlType();
            //todo: think of limits
            /*if (sqlType == SqlDbType.Decimal)
            {
                return new SqlType(sqlType, nullable);    
            }
            else if (sqlType == SqlDbType.NVarChar)
            {
                return new SqlType(sqlType, nullable);    
            }*/
            return new SqlType(sqlType, nullable);
        }
    }
}