using System;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;
using PTORMPrototype.Mapping;
using PTORMPrototype.Mapping.Configuration;
using PTORMPrototype.Query;
using PTORTMTests.TestClasses;

namespace PTORTMTests
{
    [TestFixture]
    public class QueryAndMapperTest : BaseSqlTest
    {
        private const string IdentityField = "ObjectId";

        [SetUp]
        public void Setup()
        {
            var createTable = SqlConnection.CreateCommand();
            createTable.CommandText = @"CREATE TABLE BaseClass (ObjectId uniqueidentifier PRIMARY KEY NOT NULL, _dscr int NOT NULL, Prop1 int NOT NULL);
CREATE TABLE DerivedClass (ObjectId uniqueidentifier PRIMARY KEY NOT NULL, Prop2 int NOT NULL);CREATE TABLE BaseWithNavigationClass (ObjectId uniqueidentifier PRIMARY KEY NOT NULL, _dscr int NOT NULL, Nav uniqueidentifier NULL);
CREATE TABLE NavigationedClass (ObjectId uniqueidentifier PRIMARY KEY NOT NULL, _dscr int NOT NULL, Something nvarchar(255) NULL);";
            createTable.ExecuteNonQuery();
        }


        [TearDown]
        public void TearDown()
        {
            var dropTable = SqlConnection.CreateCommand();
            dropTable.CommandText = "DROP TABLE BaseClass;DROP TABLE DerivedClass;DROP TABLE BaseWithNavigationClass;DROP TABLE NavigationedClass;";
            dropTable.ExecuteNonQuery();
        }


        [Test]       
        public void TestSimpleDerivedLoad()
        {
            //Arrange
            var insertCommand = SqlConnection.CreateCommand();
            insertCommand.CommandText = "INSERT INTO BaseClass (ObjectId, _dscr, Prop1) VALUES(@p1, 2, 1); INSERT INTO DerivedClass (ObjectId, Prop2) VALUES(@p1, 3);";
            insertCommand.Parameters.AddWithValue("@p1", Guid.NewGuid());
            insertCommand.ExecuteNonQuery();
            
            var types = FluentConfiguration.Start().DefaultIdProperty(IdentityField)
               .AddType<BaseClass>(z => z.AllProperties())
               .AddType<DerivedClass>(z => z.AllProperties())
               .GenerateTypeMappings();
            var provider = new TestProvider(types);    
            //Act
            var mapper = new SqlValueMapper();
            var builder = new QueryBuilder(provider);
            var command = SqlConnection.CreateCommand();
            var plan = builder.GetQuery("DerivedClass", new string[0]);
            BaseClass result;
            command.CommandText = plan.SqlString;
            using (var reader = command.ExecuteReader())
            {
                var classes = mapper.MapFromReader(reader, plan.SelectClause);
                result = classes.OfType<BaseClass>().First();               
            }
            //Assert            
            Assert.IsInstanceOf<DerivedClass>(result);
            Assert.AreEqual(1, result.Prop1);
        }

        [Test]
        public void TestDerivedLoadWithFilter()
        {
            //Arrange
            var insertCommand = SqlConnection.CreateCommand();
            insertCommand.CommandText = "INSERT INTO BaseClass (ObjectId, _dscr, Prop1) VALUES(@p1, 2, 1); INSERT INTO DerivedClass (ObjectId, Prop2) VALUES(@p1, 3);";
            insertCommand.Parameters.AddWithValue("@p1", Guid.NewGuid());
            insertCommand.ExecuteNonQuery();

            var types = FluentConfiguration.Start().DefaultIdProperty(IdentityField)
               .AddType<BaseClass>(z => z.AllProperties())
               .AddType<DerivedClass>(z => z.AllProperties())
               .GenerateTypeMappings();
            var provider = new TestProvider(types);
            //Act
            var mapper = new SqlValueMapper();
            var builder = new QueryBuilder(provider);
            var command = SqlConnection.CreateCommand();
            var plan = builder.GetQuery("DerivedClass", new [] { "Prop2" });
            BaseClass result;
            command.CommandText = plan.SqlString;
            command.Parameters.AddWithValue("@p0", 3);
            using (var reader = command.ExecuteReader())
            {
                var classes = mapper.MapFromReader(reader, plan.SelectClause);
                result = classes.OfType<BaseClass>().First();
            }
            //Assert            
            Assert.IsInstanceOf<DerivedClass>(result);
            Assert.AreEqual(1, result.Prop1);
        }

        [Test]
        public void TestIncludeLoad()
        {
            //Arrange            
            var insertCommand = SqlConnection.CreateCommand();
            insertCommand.CommandText = "INSERT INTO BaseWithNavigationClass (ObjectId, _dscr, Nav) VALUES(@p1, 1, @p2); INSERT INTO NavigationedClass (ObjectId, _dscr, Something) VALUES(@p2, 1, 'goody')";
            insertCommand.Parameters.AddWithValue("@p1", Guid.NewGuid());
            var navGuid = Guid.NewGuid();
            insertCommand.Parameters.AddWithValue("@p2", navGuid);
            insertCommand.ExecuteNonQuery();            

            var types = FluentConfiguration.Start()
                .DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName("_dscr")
                .AddType<BaseWithNavigationClass>(z => z.AllProperties())
                .AddType<NavigationedClass>(z => z.AllProperties())
                .GenerateTypeMappings();
            var provider = new TestProvider(types);
            var mapper = new SqlValueMapper();
            var builder = new QueryBuilder(provider);
            var command = SqlConnection.CreateCommand();
            var plan = builder.GetQuery("BaseWithNavigationClass", new string[0], new []{ "Nav"} );
            NavigationedClass result;
            command.CommandText = plan.SqlString;            
            using (var reader = command.ExecuteReader())
            {
                var classes = mapper.MapFromReader(reader, plan.SelectClause);
                result = classes.OfType<BaseWithNavigationClass>().First().Nav;
            }
            //Assert                        
            Assert.AreEqual("goody", result.Something);
        }
    }

}