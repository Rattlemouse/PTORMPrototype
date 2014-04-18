using System;
using System.Collections;
using System.Data.SqlClient;
using System.Linq;
using PTORMPrototype.Mapping.Configuration;
using PTORMPrototype.Query;

namespace PTORMPrototype.Mapping
{
    public class WriteMapper
    {
        private readonly QueryBuilder _builder;
        private readonly IMetaInfoProvider _metaInfoProvider;

        public WriteMapper(QueryBuilder builder, IMetaInfoProvider metaInfoProvider)
        {
            if (builder == null) 
                throw new ArgumentNullException("builder");
            if (metaInfoProvider == null) 
                throw new ArgumentNullException("metaInfoProvider");
            _builder = builder;
            _metaInfoProvider = metaInfoProvider;
        }

        public void InsertObject(SqlConnection connection, object newObject, object hostId = null)
        {
            if (connection == null) 
                throw new ArgumentNullException("connection");
            if (newObject == null) 
                throw new ArgumentNullException("newObject");
            var type = newObject.GetType();
            var mapping = _metaInfoProvider.GetTypeMapping(type.Name);
            var plan = _builder.GetInsert(type.Name);
            var identity = type.GetProperty(mapping.IdentityField).GetValue(newObject);
            foreach (var part in plan.Parts)
            {
                using (var command = connection.CreateCommand())
                {                    
                    command.CommandText = part.SqlString;
                    var listPart = part as PrimitiveInsertListPart;
                    if (listPart != null)
                    {
                        foreach (var column in listPart.Parameters)
                        {
                            command.Parameters.Add(column.Name, column.Property.SqlType.Type);
                        }
                        //todo: как-то хардкодно с параметрами
                        command.Parameters[0].Value = identity;
                        command.Prepare();
                        var items = (IEnumerable)(type.GetProperty(listPart.PropertyName).GetValue(newObject));
                        var i = 0;
                        foreach (var item in items)
                        {
                            command.Parameters[1].Value = item;
                            if (((PrimitiveListTable) listPart.Table).MaintainOrder)
                                command.Parameters[2].Value = i++;
                            command.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        var tbl = (EntityTable) part.Table;
                        foreach (var parameter in part.Parameters)
                        {
                            object value = null;

                            if(tbl.HasDiscriminator && tbl.DiscriminatorColumn.ColumnName == parameter.Property.ColumnName)
                                value = mapping.Discriminator;
                            else if (parameter.Property is NavigationPropertyMapping)
                            {
                                var nav = (NavigationPropertyMapping) parameter.Property;
                                if (nav.Host == ReferenceHost.Parent)
                                {
                                    var obj = type.GetProperty(parameter.Property.Name).GetValue(newObject);
                                    value = obj.GetType().GetProperty(nav.TargetType.IdentityField).GetValue(obj);
                                    InsertObject(connection, obj);
                                }
                                else if(nav.Host == ReferenceHost.Child)
                                {
                                    value = hostId;
                                }
                                else
                                {
                                    throw new NotImplementedException();
                                }
                            }
                            else
                                value = type.GetProperty(parameter.Property.Name).GetValue(newObject);
                            command.Parameters.AddWithValue(parameter.Name, value);
                        }
                        command.ExecuteNonQuery();
                    }
                }
            }
            foreach (var navs in mapping.GetNavigationProperties().Where(z => z.Host == ReferenceHost.Child && z.Table is EntityTable))
            {
                var innerObject = type.GetProperty(navs.Name).GetValue(newObject);
                var items = innerObject as IEnumerable;
                if (items != null)
                {
                    foreach (var obj in items)
                    {
                        InsertObject(connection, obj, identity);
                    }
                }
                else
                {
                    InsertObject(connection, innerObject, identity);
                }
            }
        }

        public void UpdateObject(SqlConnection connection, object newObject, string[] properties)
        {
            if (connection == null) 
                throw new ArgumentNullException("connection");
            if (newObject == null) 
                throw new ArgumentNullException("newObject");
            if (properties == null)
                throw new ArgumentNullException("properties");
            var type = newObject.GetType();
            var mapping = _metaInfoProvider.GetTypeMapping(type.Name);            
            var plan = _builder.GetUpdate(type.Name, properties);
            var identity = type.GetProperty(mapping.IdentityField).GetValue(newObject);
            foreach (var part in plan.Parts)
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = part.SqlString;
                    var listPart = part as PrimitiveInsertListPart;
                    if (listPart != null)
                    {
                        foreach (var column in listPart.Parameters)
                        {
                            command.Parameters.Add(column.Name, column.Property.SqlType.Type);
                        }
                        //todo: как-то хардкодно с параметрами
                        command.Parameters[0].Value = identity;
                        command.Prepare();
                        var items = (IEnumerable) (type.GetProperty(listPart.PropertyName).GetValue(newObject));
                        var i = 0;
                        foreach (var item in items)
                        {
                            command.Parameters[1].Value = item;
                            if (((PrimitiveListTable) listPart.Table).MaintainOrder)
                                command.Parameters[2].Value = i++;
                            command.ExecuteNonQuery();
                        }
                    }
                    else
                    {                        
                        foreach (var parameter in part.Parameters)
                        {
                            object value = null;
                            value = type.GetProperty(parameter.Property.Name).GetValue(newObject);
                            command.Parameters.AddWithValue(parameter.Name, value);
                        }
                        command.ExecuteNonQuery();
                    }
                }
            }
        }
    }

}