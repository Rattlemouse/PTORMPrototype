﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using PTORMPrototype.Mapping;
using PTORMPrototype.Mapping.Configuration;
using PTORMPrototype.Query;
using PTORTMTests.TestClasses;

namespace PTORTMTests.MassTest
{
    [TestFixture]
    [Ignore("Speedup all tests")]
    public class QueryAndMapperTest : BaseSqlTest
    {
        private const string IdentityField = "ObjectId";

        [SetUp]
        public void Setup()
        {
           
        }

        [TearDown]
        public void TearDown()
        {
            var dropTable = SqlConnection.CreateCommand();
            dropTable.CommandText = "DROP TABLE FirstClass;DROP TABLE SecondClass;DROP TABLE ThirdClass;";
            dropTable.ExecuteNonQuery();
        }
        const int Count = 100000;
        private List<Guid> LoadTable(string tableName, Action<DataRow, int> fillAction)
        {
            
            var cmd = new SqlCommand(string.Format("SET FMTONLY ON; SELECT * FROM {0}; SET FMTONLY OFF;", tableName), SqlConnection);
            var dbTable = new DataTable();
            var listGuids = new List<Guid>(Count);
            dbTable.Load(cmd.ExecuteReader());
            dbTable.BeginLoadData();
            for (int i = 0; i < Count; i++)
            {
                var dataRow = dbTable.NewRow();
                listGuids.Add(FillDataRow(dataRow));
                fillAction(dataRow, i);
                dbTable.Rows.Add(dataRow);
            }
            dbTable.EndLoadData();
            var bulkCopy = new SqlBulkCopy(SqlConnection);
            bulkCopy.DestinationTableName = tableName;
            bulkCopy.WriteToServer(dbTable, DataRowState.Added);
            bulkCopy.Close();
            return listGuids;
        }

        [Test]
        public void Test()
        {
            var mappings = FluentConfiguration.Start().DefaultDiscriminatorColumnName("_dscr")
                .DefaultIdProperty(IdentityField)
                .AddTypeAuto<FirstClass>()
                .AddTypeAuto<SecondClass>()
                .AddTypeAuto<ThirdClass>().GenerateTypeMappings();
            
            var provider = new TestProvider(mappings);
            var queryBuilder = new QueryBuilder(provider);
            var createTable = SqlConnection.CreateCommand();
            createTable.CommandText = string.Concat(
                queryBuilder.GetCreateTable(typeof(FirstClass)),
                queryBuilder.GetCreateTable(typeof(SecondClass)),
                queryBuilder.GetCreateTable(typeof(ThirdClass))
            );
            createTable.ExecuteNonQuery();
            var thirdGuids = LoadTable("ThirdClass", (row, i) => { });
            var secondGuids = LoadTable("SecondClass", (row, i) => { row["Third"] = thirdGuids[i]; });
            LoadTable("FirstClass", (row, i) => { row["Second"] = secondGuids[i]; });            
            
           

            var stopWatch = new Stopwatch();
            stopWatch.Start();

          
            var plan = queryBuilder.GetQuery("FirstClass", new string[0], new[] { "Second.Third" });
            Debug.WriteLine(plan.SqlString);

            var mapper = new SqlValueMapper();
            var firstClasses = new List<FirstClass>(Count);
            using (var cmd = new SqlCommand(plan.SqlString, SqlConnection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    firstClasses.AddRange(mapper.MapFromReader(reader, plan.SelectClause).OfType<FirstClass>());
                }                
            }
            stopWatch.Stop();
            Assert.AreEqual(Count, firstClasses.Count(z => z.Second != null && z.Second.Third != null));
            Assert.Pass(stopWatch.Elapsed.ToString());            
        }        

        private Guid FillDataRow(DataRow row)
        {
            var newGuid = Guid.NewGuid();
            row["ObjectId"] = newGuid;
            row["_dscr"] = 1;
            row["FP1"] = 1;
            row["FP2"] = 2;
            row["FP3"] = 3;
            row["FP4"] = 4;
            row["FP5"] = "501";
            row["FP6"] = "502";
            row["FP7"] = "503";
            row["FP8"] = "504";
            return newGuid;
        }
    }
}
