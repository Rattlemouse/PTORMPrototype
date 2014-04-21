using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using PTORMPrototype.Mapping;
using PTORMPrototype.Mapping.Configuration;
using PTORMPrototype.Query.Sql;

namespace PTORMPrototype.Query
{
    public class QueryBuilder
    {
        private readonly IMetaInfoProvider _metaInfoProvider;
        private readonly Dictionary<string, AliasContext> _contextAliases = new Dictionary<string, AliasContext>();
       
        public QueryBuilder(IMetaInfoProvider metaInfoProvider)
        {
            if (metaInfoProvider == null) 
                throw new ArgumentNullException("metaInfoProvider");
            _metaInfoProvider = metaInfoProvider;
        }

        private TableContext GetTableContext(Table table, string context)
        {            
            AliasContext aliasContext;
            if (!_contextAliases.TryGetValue(context, out aliasContext))
            {
                aliasContext = new AliasContext(context);
                _contextAliases.Add(context, aliasContext);
            }
            return new TableContext(table, aliasContext.GetTableAlias(table.Name));
        }
        public QueryPlan GetQuery(string type, string[] paths, string[] includes = null)
        {
            var typeMapping = _metaInfoProvider.GetTypeMapping(type);
            var tables = typeMapping.Tables.OfType<EntityTable>().ToList();
            var mainTable = tables.First();
            var mainTableContext = GetTableContext(mainTable, "M");
            var selectParts = new List<SelectPart>();
            var selectBuilder = new SelectSqlBuilder(mainTableContext).SelectAll(mainTableContext);
            var mainPart = new TypePart
            {
                Type =  typeMapping,
                Tables = new List<Table>(tables)
            };
            selectParts.Add(mainPart);            
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    selectParts.AddRange(Include(selectBuilder, mainTableContext, typeMapping, include));
                }
            }
            
            foreach (var table in tables.Skip(1))
            {
                var lastContext = GetTableContext(table, "T");
                selectBuilder
                    .SelectAll(lastContext)
                    .InnerJoin(mainTableContext, lastContext, table.IdentityColumn.ColumnName, table.IdentityColumn.ColumnName);
            }
            foreach (var s in paths)
            {
                BuildSelectPredicate(selectBuilder, mainTableContext, typeMapping, s);
            }
           
            var selectClause = new SelectClause{ Parts = selectParts.ToArray() };
            return new QueryPlan
            {
                SqlString = selectBuilder.GetSql(),
                SelectClause = selectClause
            };
        }

        private IEnumerable<SelectPart> Include(SelectSqlBuilder bulder, TableContext mainTableContext, TypeMappingInfo typeMapping, string include)
        {
            var fields = include.Split('.');
            var currentType = typeMapping;
            var context = mainTableContext;
            foreach (var navigationField in fields)
            {
                string typeCast;
                var field = FindTypeCast(navigationField, out typeCast);
                var navProperty = (NavigationPropertyMapping)currentType.GetProperty(field);
                if (navProperty.Table is PrimitiveListTable)
                {
                    var listContext = GetTableContext(navProperty.Table, "S");
                    bulder.SelectAll(listContext);
                    bulder.LeftOuterJoin(context, listContext, ((EntityTable)context.Table).IdentityColumn.ColumnName, navProperty.Table.Columns.First().ColumnName);                    
                    yield return new ExpansionPart
                    {                        
                        Table = navProperty.Table,
                        CollectingType = currentType,
                        CollectingProperty = navProperty
                    };
                    break;
                }                
                var targetType = navProperty.TargetType;                
                var tableInfo = targetType.Tables.OfType<EntityTable>().First();
                var nextcontext = GetTableContext(tableInfo, "S");
                bulder.SelectAll(nextcontext);
                if (navProperty.Host == ReferenceHost.Parent)
                {
                    bulder.LeftOuterJoin(context, nextcontext, navProperty.ColumnName, tableInfo.IdentityColumn.ColumnName);
                }
                else if (navProperty.Host == ReferenceHost.Child)
                {
                    bulder.LeftOuterJoin(context, nextcontext, ((EntityTable)context.Table).IdentityColumn.ColumnName, navProperty.ColumnName);
                }
                else
                    throw new NotImplementedException();
                var tables = new List<Table> { tableInfo };
                if (typeCast != null)
                {
                    var castedType = _metaInfoProvider.GetTypeMapping(typeCast);
                    var remainTables = castedType.Tables.OfType<EntityTable>().SkipWhile(z => z != tableInfo).Skip(1);                    
                    foreach (var nextTable in remainTables)
                    {
                        var ctx = GetTableContext(nextTable, "S");
                        bulder.SelectAll(ctx);
                        bulder.InnerJoin(nextcontext, ctx, tableInfo.IdentityColumn.ColumnName,
                            tableInfo.IdentityColumn.ColumnName);
                        nextcontext = ctx;
                        tables.Add(nextTable);
                    }
                    targetType = castedType;
                }

                yield return new SubTypePart
                {
                    Type = targetType,
                    Tables = tables,
                    CollectingType = currentType,
                    CollectingProperty = navProperty
                };
                currentType = targetType;
                context = nextcontext;
            }
        }        

        private void BuildSelectPredicate(SelectSqlBuilder builder, TableContext initialContext, TypeMappingInfo type, string s)
        {
            var fields = s.Split('.');            
            var searchField = fields.Last();         
            var entityTable = type.Tables.OfType<EntityTable>().Last();
            var searchContext = GetTableContext(entityTable, initialContext.Table == entityTable ? "M" : "T");            

            var contexts = GetContexts(builder, type, searchContext, fields.Take(fields.Length - 1).ToList());
            
            var parameter = builder.GetNextParameter();
            var predicates = new List<string>();
            foreach (var contextTuple in contexts)
            {
                var currentType = contextTuple.Item1;                                
                var context = contextTuple.Item2;
                if (currentType.HasProperty(searchField))
                {
                    var property = currentType.GetProperty(searchField);
                    var tbl = property.Table;
                    if (tbl == initialContext.Table)
                        context = initialContext;
                    string predicate;
                    if (tbl is PrimitiveListTable)
                    {
                        predicate = PredicateForPrimitiveList(builder, tbl, context, parameter);
                    }
                    else
                    {
                        predicate = builder.GetEquality(context, property.ColumnName, parameter);
                    }

                    predicates.Add(predicate);
                }
                else
                {
                    var branchNumber = 1;
                    foreach (var childType in currentType.GetNearestChildrenThatHaveProperty(searchField))
                    {
                        var contextPrefix = context.Alias + "B" + branchNumber + "T";
                        var childTablesLeft = childType.Tables.SkipWhile(z => z != context.Table).ToList();
                        var previousContext = context;
                        var property = childType.GetProperty(searchField);
                        var tbl = property.Table;
                        var tableContext = GetTableContext(tbl, contextPrefix);                                             
                        foreach (var tableToJoin in childTablesLeft.Skip(1))
                        {
                            var currentContext = GetTableContext(tableToJoin, contextPrefix);
                            builder.InnerJoin(previousContext, currentContext,
                                ((EntityTable)previousContext.Table).IdentityColumn.ColumnName,
                                ((EntityTable)currentContext.Table).IdentityColumn.ColumnName);
                            previousContext = currentContext;
                        }
                        
                        string predicate;
                        if (tbl is PrimitiveListTable)
                        {
                            predicate = PredicateForPrimitiveList(builder, tbl, tableContext, parameter);
                        }
                        else
                        {
                            predicate = builder.GetEquality(tableContext, property.ColumnName, parameter);
                        }

                        predicates.Add(predicate);
                        branchNumber++;
                    }                    
                }
            }
            if (predicates.Count == 0)
                throw new InvalidOperationException(string.Format("Wrong query, no types of {1} with field {0} found", s, type.Type));
            //todo: think of context and inheritance
            builder.Where(builder.Or(predicates));
        }

        private string FindTypeCast(string searchField, out string typeCast)
        {
            if (searchField.Contains('['))
            {
                var idx = searchField.IndexOf('[');
                typeCast = searchField.Substring(idx + 1, searchField.IndexOf(']') - idx - 1);
                return searchField.Substring(0, idx);
            }
            typeCast = null;
            return searchField;
        }

        private IEnumerable<Tuple<TypeMappingInfo, TableContext>> GetContexts(SelectSqlBuilder builder, TypeMappingInfo type, TableContext context, IList<string> properties, string cast = null)
        {
            if (properties.Count == 0)
            {
                yield return Tuple.Create(type, context);                
                yield break;
            }
            var currentProperty = properties[0];

            string typeCast;
            currentProperty = FindTypeCast(currentProperty, out typeCast);

            var remainingProperties = properties.Skip(1).ToList();
            if (type.HasProperty(currentProperty))
            {
                if (cast != null && type.Type.Name != cast)
                    yield break;               
                var prefix = context.Alias + "B0T";
                var navProperty = (NavigationPropertyMapping) type.GetProperty(currentProperty);
                foreach (var tuple in GetContextFromType(builder, navProperty, prefix, context, remainingProperties, typeCast))
                {
                    yield return tuple;
                }                
            }
            else
            {
                var branchNumber = 1;
                foreach (var childType in type.GetNearestChildrenThatHaveProperty(currentProperty).Where(z => cast == null || cast == z.Type.Name))
                {                    
                    var contextPrefix = context.Alias + "B" + branchNumber + "T";
                    var childTablesLeft = childType.Tables.SkipWhile(z => z != context.Table).ToList();
                    var baseChildTable = childTablesLeft[0];
                    var previousContext = GetTableContext(baseChildTable, contextPrefix);
                    if(!_contextAliases.ContainsKey(baseChildTable.Name))
                    builder.LeftOuterJoin(context, previousContext,
                        ((EntityTable)context.Table).IdentityColumn.ColumnName,
                        ((EntityTable)previousContext.Table).IdentityColumn.ColumnName);
                    foreach (var tableToJoin in childTablesLeft.Skip(1))
                    {
                        var currentContext = GetTableContext(tableToJoin, contextPrefix);
                        if (!_contextAliases.ContainsKey(tableToJoin.Name))
                        builder.InnerJoin(previousContext, currentContext,
                            ((EntityTable) previousContext.Table).IdentityColumn.ColumnName,
                            ((EntityTable) currentContext.Table).IdentityColumn.ColumnName);
                        previousContext = currentContext;
                    }
                    var navProperty = (NavigationPropertyMapping) childType.GetProperty(currentProperty);
                    foreach (
                        var tuple1 in
                            GetContextFromType(builder, navProperty, contextPrefix, previousContext, remainingProperties, typeCast)
                        )
                    {
                       yield return tuple1;
                    }
                    branchNumber++;
                }
            }
        }

        private IEnumerable<Tuple<TypeMappingInfo, TableContext>> GetContextFromType(SelectSqlBuilder builder, NavigationPropertyMapping navProperty, string contextPrefix, TableContext previousContext, List<string> remainingProperties, string typeCast)
        {            
            var currentType = navProperty.TargetType;
            var tableInfo = currentType.Tables.OfType<EntityTable>().First();
            var tableContext = GetTableContext(tableInfo, contextPrefix);
            if (navProperty.Host == ReferenceHost.Parent)
            {
                builder.InnerJoin(previousContext, tableContext, navProperty.ColumnName, tableInfo.IdentityColumn.ColumnName);
            }
            else if (navProperty.Host == ReferenceHost.Child)
            {
                builder.InnerJoin(previousContext, tableContext, ((EntityTable) previousContext.Table).IdentityColumn.ColumnName,
                    navProperty.ColumnName);
            }
            foreach (var tuple in GetContexts(builder, currentType, tableContext, remainingProperties, typeCast))
            {
                yield return tuple;
            }
        }

        private string PredicateForPrimitiveList(SelectSqlBuilder builder, Table tbl, TableContext context, string parameter)
        {
            var listContext = GetTableContext(tbl, "T");
            builder.InnerJoin(context, listContext, ((EntityTable) context.Table).IdentityColumn.ColumnName,
                tbl.Columns[0].ColumnName);
            return builder.GetEquality(listContext, tbl.Columns[1].ColumnName, parameter);            
        }


        public UpdatePlan GetUpdate(string type, string[] properties)
        {
            var typeMapping = _metaInfoProvider.GetTypeMapping(type);            
            var updateParts = new List<UpdatePart>();
            var props = properties.Select(typeMapping.GetProperty).GroupBy(z => z.Table);
            foreach (var propGroup in props)
            {
                UpdatePart updatePart;
                if (propGroup.Key is EntityTable)
                {
                    updatePart = GetUpdateForEntity((EntityTable) propGroup.Key, propGroup);
                }
                else
                {
                    var primitiveListTable = propGroup.Key as PrimitiveListTable;
                    if (primitiveListTable != null)
                    {
                        updateParts.Add(GetDeleteForPrimitiveListTabe(primitiveListTable, propGroup));
                        updatePart = GetInsertForPrimitiveListTable(primitiveListTable, propGroup);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                updatePart.Table = propGroup.Key;
                updateParts.Add(updatePart);
                //todo: don't like this line :(
                _contextAliases.Clear();
            }            
            return new UpdatePlan {Parts = updateParts};
        }

        private UpdatePart GetDeleteForPrimitiveListTabe(PrimitiveListTable primitiveListTable, IGrouping<Table, PropertyMapping> propGroup)
        {
            var mainTableContext = GetTableContext(primitiveListTable, "M");
            var builder = new DeleteSqlBuilder(mainTableContext);
            var nextParameter = builder.GetNextParameter();
            var equalPredicate = builder.GetEquality(mainTableContext, primitiveListTable.Columns.First().ColumnName, nextParameter);
            builder.Where(equalPredicate);
            return new UpdatePart { SqlString = builder.GetSql(), Parameters = { new Parameter { Name = nextParameter, Property = propGroup.First(z => z.DeclaredType != null).DeclaredType.Tables.OfType<EntityTable>().First().IdentityColumn} } };
        }


        private UpdatePart GetUpdateForEntity(EntityTable mainTable,IEnumerable<PropertyMapping> propGroup)
        {
            var mainTableContext = GetTableContext(mainTable, "M");
            var updatebuilder = new UpdateSqlBuilder(mainTableContext);
            var param = updatebuilder.GetNextParameter();
            updatebuilder.Where(
                updatebuilder.GetEquality(mainTableContext, mainTable.IdentityColumn.ColumnName, param)
                );
            var updatePart = new UpdatePart
            {
                Parameters = {new Parameter {Property = mainTable.IdentityColumn, Name = param}}
            };
            foreach (var prop in propGroup)
            {                
                param = updatebuilder.Set(mainTableContext, prop.ColumnName);
                updatePart.Parameters.Add(new Parameter{ Name = param, Property = prop});
            }
            updatePart.SqlString = updatebuilder.GetSql();
            return updatePart;
        }

        public UpdatePlan GetInsert(string type)
        {
            var typeMapping = _metaInfoProvider.GetTypeMapping(type);
            var updateParts = new List<UpdatePart>();
            var selectedColumns = typeMapping.Tables.SelectMany(z => z.Columns).ToList();
            var props = typeMapping.Tables.OfType<EntityTable>().Select(z => z.IdentityColumn).Concat(
                                                selectedColumns
            ).Concat(
                typeMapping.GetProperties().Where(p => selectedColumns.All(a => a.Name != p.Name))
            ).GroupBy(z => z.Table);
            foreach (var propGroup in props.Where(z => z.Key is PrimitiveListTable || typeMapping.Tables.Contains(z.Key) ))
            {
                UpdatePart updatePart;
                if (propGroup.Key is EntityTable)
                {
                    updatePart = GetInsertForEntity((EntityTable)propGroup.Key, propGroup);
                }
                else if (propGroup.Key is PrimitiveListTable)
                {
                    updatePart = GetInsertForPrimitiveListTable((PrimitiveListTable)propGroup.Key, propGroup);
                }
                else
                {
                    throw new NotImplementedException();
                }
                updatePart.Table = propGroup.Key;
                updateParts.Add(updatePart);                
            }
            return new UpdatePlan { Parts = updateParts };
        }

        private UpdatePart GetInsertForPrimitiveListTable(PrimitiveListTable primitiveListTable, IEnumerable<PropertyMapping> propGroup)
        {
            var insertBuilder = new InsertSqlBuilder(primitiveListTable);
            var updatePart = new PrimitiveInsertListPart();
            foreach (var prop in primitiveListTable.Columns)
            {
                var paramerter = new Parameter()
                {
                    Name = insertBuilder.AddInsert(prop.ColumnName),
                    Property = prop
                };
                updatePart.Parameters.Add(paramerter);
            }
            updatePart.SqlString = insertBuilder.GetSql();            
            updatePart.PropertyName = propGroup.First(z => z.DeclaredType != null).Name;
            return updatePart;
        }

        private static UpdatePart GetInsertForEntity(EntityTable mainTable, IEnumerable<PropertyMapping> propGroup)
        {
            var insertBuilder = new InsertSqlBuilder(mainTable);
            var updatePart = new UpdatePart();
            if (mainTable.HasDiscriminator)
            {
                propGroup = Enumerable.Repeat(mainTable.DiscriminatorColumn, 1).Concat(propGroup);
            }            
            foreach (var prop in propGroup)
            {
                var paramerter = new Parameter()
                {
                    Name = insertBuilder.AddInsert(prop.ColumnName),
                    Property = prop
                };
                updatePart.Parameters.Add(paramerter);
            }
            updatePart.SqlString = insertBuilder.GetSql();
            return updatePart;
        }

        public string GetCreateTable(Type type)
        {
            return new CreateTableBuilder(_metaInfoProvider.GetTypeMapping(type.Name)).GetSql();
        }
    }
}