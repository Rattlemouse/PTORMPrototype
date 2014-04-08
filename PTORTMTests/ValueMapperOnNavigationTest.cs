using System;
using System.Linq;
using NUnit.Framework;
using PTORMPrototype.Mapping;
using PTORMPrototype.Mapping.Configuration;
using PTORMPrototype.Query;
using PTORTMTests.TestClasses;

namespace PTORTMTests
{
    [TestFixture]
    public class ValueMapperOnNavigationTest : BaseSqlTest
    {
       
        private const string IdentityField = "ObjectId";

        [SetUp]
        public void Setup()
        {            
            var createTable = SqlConnection.CreateCommand();
            createTable.CommandText = @"CREATE TABLE BaseWithNavigationClass (ObjectId uniqueidentifier PRIMARY KEY NOT NULL, _dscr int NOT NULL, Nav uniqueidentifier NULL);
CREATE TABLE NavigationedClass (ObjectId uniqueidentifier PRIMARY KEY NOT NULL, Something nvarchar(255) NULL);";
            createTable.ExecuteNonQuery();
        }

        [TearDown]
        public void TearDown()
        {
            var dropTable = SqlConnection.CreateCommand();
            dropTable.CommandText = "DROP TABLE BaseWithNavigationClass;DROP TABLE NavigationedClass;";
            dropTable.ExecuteNonQuery();            
        }


        [Test]
        public void TestSimpleClassMapping()
        {
            var insertCommand = SqlConnection.CreateCommand();
            insertCommand.CommandText = "INSERT INTO BaseWithNavigationClass (ObjectId, _dscr, Nav) VALUES(@p1, 1, @p2)";
            insertCommand.Parameters.AddWithValue("@p1", Guid.NewGuid());
            var navGuid = Guid.NewGuid();
            insertCommand.Parameters.AddWithValue("@p2", navGuid);
            insertCommand.ExecuteNonQuery();
            var command = SqlConnection.CreateCommand();
            command.CommandText = "SELECT [T1].* FROM [BaseWithNavigationClass] AS [T1]";

            var types = FluentConfiguration.Start()
                .DefaultIdProperty(IdentityField)
                .AddType<BaseWithNavigationClass>(z => z.AllProperties())
                .AddType<NavigationedClass>(z => z.AllProperties())
                .GenerateTypeMappings();
            var provider = new TestProvider(types);
            var mapper = new SqlValueMapper();
            using (var reader = command.ExecuteReader())
            {
                var typeMappingInfo = provider.GetTypeMapping("BaseWithNavigationClass");
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
                var result = classes.OfType<BaseWithNavigationClass>().First();
                Assert.IsNotNull(result.Nav);
                Assert.AreEqual(navGuid, result.Nav.ObjectId);
            }
        }
            }
        }