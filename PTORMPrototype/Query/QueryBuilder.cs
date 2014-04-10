using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using PTORMPrototype.Mapping;
using PTORMPrototype.Mapping.Configuration;

namespace PTORMPrototype.Query
{
    public class QueryBuilder
    {
        private class AliasContext
        {
            readonly Dictionary<string, string> _aliases = new Dictionary<string, string>();
            private readonly string _context;

            public AliasContext(string context)
            {
                _context = context;
            }

            public string GetTableAlias(string tableName)
            {
                string alias;
                if (_aliases.TryGetValue(tableName, out alias))
                    return alias;
                alias = string.Format("[{1}{0}]", _aliases.Count + 1, _context);
                _aliases.Add(tableName, alias);
                return alias;
            }
        }

        private class TableContext
        {
            private readonly Table _table;
            private readonly string _alias;

            public TableContext(Table table, string alias)
            {
                _table = table;
                _alias = alias;
            }

            public Table Table
            {
                get { return _table; }
            }

            public string Alias
            {
                get { return _alias; }
            }
        }


        private readonly IMetaInfoProvider _metaInfoProvider;
        private int _currentParameter = 0;
        private readonly Dictionary<string, AliasContext> _contextAliases = new Dictionary<string, AliasContext>();


        private readonly List<string> _selectClauses = new List<string>();
        private string _fromClause;
        private readonly List<string> _joinClauses = new List<string>();
        private readonly List<string> _whereClauses = new List<string>();

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
            SelectAll(mainTableContext).From(mainTableContext);
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
                    selectParts.AddRange(Include(mainTableContext, typeMapping, include));
                }
            }
            foreach (var table in tables.Skip(1))
            {                
                var secondContext = GetTableContext(table, "T");
                SelectAll(secondContext);
                InnerJoin(mainTableContext, secondContext, table.IdentityColumn.ColumnName, table.IdentityColumn.ColumnName);
            }
            foreach (var s in paths)
            {
                Where(GetPredicate(mainTableContext, typeMapping, s));
            }
           
            var selectClause = new SelectClause{ Parts = selectParts.ToArray() };
            return new QueryPlan
            {
                SqlString = ToString(),
                SelectClause = selectClause
            };
        }

        private IEnumerable<SelectPart> Include(TableContext mainTableContext, TypeMappingInfo typeMapping, string include)
        {
            var fields = include.Split('.');
            var currentType = typeMapping;
            var context = mainTableContext;
            foreach (var navigationField in fields)
            {
                var navProperty = (NavigationPropertyMapping) currentType.GetProperty(navigationField);
                if (navProperty.Table is PrimitiveListTable)
                {
                    var listContext = GetTableContext(navProperty.Table, "S");
                    SelectAll(listContext);
                    LeftOuterJoin(context, listContext, ((EntityTable)context.Table).IdentityColumn.ColumnName, navProperty.Table.Columns.First().ColumnName);                    
                    yield return new ExpansionPart
                    {                        
                        Table = navProperty.Table,
                        CollectingType = currentType,
                        CollectingProperty = navProperty
                    };
                    yield break;
                }                
                
                //todo: no primitive collections totally and no inheritance lookup
                var targetType = navProperty.TargetType;
                var tableInfo = targetType.Tables.OfType<EntityTable>().First();
                var nextcontext = GetTableContext(tableInfo, "S");
                SelectAll(nextcontext);

                if (navProperty.Host == ReferenceHost.Parent)
                {
                    LeftOuterJoin(context, nextcontext, navProperty.ColumnName, tableInfo.IdentityColumn.ColumnName);
                }
                else if (navProperty.Host == ReferenceHost.Child)
                {
                    LeftOuterJoin(context, nextcontext, ((EntityTable) context.Table).IdentityColumn.ColumnName, navProperty.ColumnName);
                }
                else
                    throw new NotImplementedException();
                yield return new SubTypePart
                {
                    Type = targetType,
                    Tables = new[] {tableInfo},
                    CollectingType = currentType,
                    CollectingProperty = navProperty
                };
                currentType = targetType;
                context = nextcontext;
            }
        }

        private string GetPredicate(TableContext initialContext, TypeMappingInfo type, string s)
        {
            var fields = s.Split('.');
            var currentType = type;
            var context = initialContext;
            foreach (var navigationField in fields.Take(fields.Length - 1))
            {
                var navProperty = (NavigationPropertyMapping) type.GetProperty(navigationField);
                var table = navProperty.Table;
                if (table is PrimitiveListTable)
                {
                    return GetForPrimitiveList(table, context);
                }
                currentType = navProperty.TargetType;
                //todo: no primitive collections totally and no inheritance lookup
                var tableInfo = currentType.Tables.OfType<EntityTable>().First();
                var nextcontext = GetTableContext(tableInfo, "T");
                if (navProperty.Host == ReferenceHost.Parent)
                {
                    InnerJoin(context, nextcontext, navProperty.ColumnName, tableInfo.IdentityColumn.ColumnName);
                }
                else if (navProperty.Host == ReferenceHost.Child)
                {
                    InnerJoin(context, nextcontext, ((EntityTable) context.Table).IdentityColumn.ColumnName, navProperty.ColumnName);
                }
                else
                    throw new NotImplementedException();
                
                context = nextcontext;
            }
            var property = currentType.GetProperty(fields.Last());
            var tbl = property.Table;
            if (tbl is PrimitiveListTable)
            {
                return GetForPrimitiveList(tbl, context);
            }
            //todo: think of context and inheritance
            return string.Format("{0} = {1}", Column(GetTableContext(tbl, tbl == initialContext.Table ? "M" : "T"), property.ColumnName), GetNextParameter());
        }

        private string GetForPrimitiveList(Table table, TableContext context)
        {
            var listContext = GetTableContext(table, "T");
            InnerJoin(context, listContext, ((EntityTable) context.Table).IdentityColumn.ColumnName, table.Columns[0].ColumnName);
            return string.Format("{0} = {1}", Column(listContext, table.Columns[1].ColumnName), GetNextParameter());
        }

        private static string Column(TableContext table, string column)
        {
            return string.Format("{0}.[{1}]", table.Alias, column);
        }

        private QueryBuilder InnerJoin(TableContext firstContext, TableContext secondContext, string name, string identityField)
        {
            _joinClauses.Add(string.Format("INNER JOIN [{0}] AS {1} ON {2} = {3}",
                  secondContext.Table.Name,
                  secondContext.Alias,
                Column(firstContext, name),
                Column(secondContext, identityField)
                ));
            return this;
        }

        private QueryBuilder LeftOuterJoin(TableContext firstContext, TableContext secondContext, string name, string identityField)
        {
            _joinClauses.Add(string.Format("LEFT OUTER JOIN [{0}] AS {1} ON {2} = {3}",
                  secondContext.Table.Name,
                  secondContext.Alias,
                Column(firstContext, name),
                Column(secondContext, identityField)
            ));
            return this;
        }

        private string GetNextParameter()
        {
            return string.Format("@p{0}", _currentParameter++);
        }

        private QueryBuilder Where(string getEqualPredicate)
        {
            _whereClauses.Add(getEqualPredicate);
            return this;
        }

        private QueryBuilder From(TableContext context)
        {
            _fromClause = "FROM [" + context.Table.Name + "] AS " + context.Alias;
            return this;
        }

        private QueryBuilder SelectAll(TableContext context)
        {
            this._selectClauses.Add(string.Format("{0}.*", context.Alias));
            return this;
        }

        public override string ToString()
        {
            var builder = new StringBuilder("SELECT ");
            builder.Append(string.Join(", ", _selectClauses));
            builder.Append(" ");
            builder.Append(_fromClause);
            foreach (var joinClause in _joinClauses)
            {
                builder.Append(" ");
                builder.Append(joinClause);
            }
            if (_whereClauses.Count > 0)
            {
                builder.Append(" WHERE ");
                builder.Append(string.Join(" AND ", _whereClauses));
            }
            return builder.ToString();
        }

        public string GetCreateTable(Type type)
        {
            var typeMapping = _metaInfoProvider.GetTypeMapping(type.Name);
            var table = typeMapping.Tables.OfType<EntityTable>().Last();            
            return string.Join("\n", new [] { CreateTable(table) }.Concat(typeMapping.Tables.OfType<PrimitiveListTable>().Select(CreateTable)));
        }

        private static string CreateTable(Table table)
        {
            var stringBuilder = new StringBuilder("CREATE TABLE ");
            stringBuilder.AppendFormat("[{0}]", table.Name);
            stringBuilder.Append(" (");
            var columns = new List<string>(table.Columns.Count + 2);               
            var entityTabe = table as EntityTable;
            if (entityTabe != null)
            {
                columns.Add(string.Format("[{0}] {1} PRIMARY KEY", entityTabe.IdentityColumn.ColumnName,
                    entityTabe.IdentityColumn.SqlType.ToString().ToUpper()));
                if (entityTabe.HasDiscriminator)
                {
                    columns.Add(string.Format("[{0}] INT NOT NULL", entityTabe.DiscriminatorColumn));
                }                                
            }
            columns.AddRange(table.Columns.Select(column => string.Format("[{0}] {1}", column.ColumnName, column.SqlType.ToString().ToUpper())));
            stringBuilder.Append(string.Join(", ", columns));
            stringBuilder.Append(");");
            return stringBuilder.ToString();
        }
    }
}