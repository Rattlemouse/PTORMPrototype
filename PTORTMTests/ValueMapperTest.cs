using System;
using System.Data.SqlClient;
using System.Linq;
using NUnit.Framework;
using PTORMPrototype.Mapping;
using PTORMPrototype.Mapping.Configuration;
using PTORMPrototype.Query;
using PTORTMTests.TestClasses;

namespace PTORTMTests
{
    [TestFixture]
    public class ValueMapperTest : BaseSqlTest
    {
        private const string IdentityField = "ObjectId";
      
        [SetUp]
        public void Setup()
        {
            var createTable = SqlConnection.CreateCommand();
            createTable.CommandText = @"CREATE TABLE BaseClass (ObjectId uniqueidentifier PRIMARY KEY NOT NULL, _dscr int NOT NULL, Prop1 int NOT NULL);
CREATE TABLE DerivedClass (ObjectId uniqueidentifier PRIMARY KEY NOT NULL, Prop2 int NOT NULL);";
            createTable.ExecuteNonQuery();
        }


        [TearDown]
        public void TearDown()
        {
            var dropTable = SqlConnection.CreateCommand();
            dropTable.CommandText = "DROP TABLE BaseClass;DROP TABLE DerivedClass;";
            dropTable.ExecuteNonQuery();            
        }

        [Test]
        public void TestDerivedClassMapping()
        {                 
            var insertCommand = SqlConnection.CreateCommand();
            insertCommand.CommandText = "INSERT INTO BaseClass (ObjectId, _dscr, Prop1) VALUES(@p1, 2, 1); INSERT INTO DerivedClass (ObjectId, Prop2) VALUES(@p1, 3);";
            insertCommand.Parameters.AddWithValue("@p1", Guid.NewGuid());            
            insertCommand.ExecuteNonQuery();
            var command = SqlConnection.CreateCommand();
            command.CommandText = "SELECT [T1].*, [T2].* FROM [BaseClass] AS [T1] INNER JOIN [DerivedClass] AS [T2] ON [T1].[ObjectId] = [T2].[ObjectId];";
            var types = FluentConfiguration.Start().DefaultIdProperty(IdentityField)
                .AddType<BaseClass>(z => z.AllProperties())
                .AddType<DerivedClass>(z => z.AllProperties())
                .GenerateTypeMappings();
            var provider = new TestProvider(types);    
            
            var mapper = new SqlValueMapper();
            using (var reader = command.ExecuteReader())
            {
                var typeMappingInfo = provider.GetTypeMapping("DerivedClass");
                var selectClause = new SelectClause
                {
                    Parts = new SelectPart[]
                    {
                        new TypePart
                        {
                            Type = typeMappingInfo, 
                            Tables = typeMappingInfo.Tables.ToList()
                        }
                    }
                };
                var classes = mapper.MapFromReader(reader, selectClause);
                var result = classes.OfType<BaseClass>().First();
                Assert.AreEqual(1, result.Prop1);
            }
        }

        [Test]
        public void TestSimpleClassMapping()
        {
            var insertCommand = SqlConnection.CreateCommand();
            insertCommand.CommandText = "INSERT INTO BaseClass (ObjectId, _dscr, Prop1) VALUES(@p1, 1, @p2)";
            insertCommand.Parameters.AddWithValue("@p1", Guid.NewGuid());
            insertCommand.Parameters.AddWithValue("@p2", 1);
            insertCommand.ExecuteNonQuery();
            var command = SqlConnection.CreateCommand();
            command.CommandText = "SELECT [T1].* FROM [BaseClass] AS [T1]";            

            var types = FluentConfiguration.Start().DefaultIdProperty(IdentityField).AddType<BaseClass>(z => z.AllProperties()).GenerateTypeMappings();
            var provider = new TestProvider(types);    
            
            var mapper = new SqlValueMapper();
            using (var reader = command.ExecuteReader())
            {
                var typeMappingInfo = provider.GetTypeMapping("BaseClass");
                var selectClause = new SelectClause
                {
                    Parts = new SelectPart[]
                    {
                        new TypePart
                        {
                            Type = typeMappingInfo, 
                            Tables = typeMappingInfo.Tables.Take(1).ToList()
                        }
                    }
                };
                var classes = mapper.MapFromReader(reader, selectClause);
                var result = classes.OfType<BaseClass>().First();
                Assert.AreEqual(1, result.Prop1);
            }
        }
    }

}

