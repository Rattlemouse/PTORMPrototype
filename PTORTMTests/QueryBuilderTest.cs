using System.Linq;
using NUnit.Framework;
using PTORMPrototype.Mapping.Configuration;
using PTORMPrototype.Query;
using PTORTMTests.TestClasses;

namespace PTORTMTests
{
    [TestFixture]
    public class QueryBuilderTest
    {
        private const string IdentityField = "ObjectId";

        [Test]
        public void TestSimpleQuery()
        {
            const string type = "BaseClass";
            const string path = "Prop1";            
            var types = FluentConfiguration.Start().DefaultIdProperty(IdentityField).AddType<BaseClass>(z => z.AllProperties()).GenerateTypeMappings();
            var provider = new TestProvider(types);            
            var queryBuilder = new QueryBuilder(provider);
            var plan = queryBuilder.GetQuery(type, new[]
            {
                path
            });
            Assert.AreEqual("SELECT [M1].* FROM [BaseClass] AS [M1] WHERE [M1].[Prop1] = @p0", plan.SqlString);
            Assert.AreEqual(1, plan.SelectClause.Parts.OfType<TypePart>().Count());
            Assert.IsTrue(plan.SelectClause.Parts.OfType<TypePart>().All(z => z.Tables.Count == 1));
        }

        [Test]
        public void TestCreateTableQuery()
        {        
            var types = FluentConfiguration.Start().DefaultIdProperty(IdentityField).DefaultDiscriminatorColumnName("_dscr").AddType<BaseClass>(z => z.AllProperties()).GenerateTypeMappings();
            var provider = new TestProvider(types);
            var queryBuilder = new QueryBuilder(provider);
            var result = queryBuilder.GetCreateTable(typeof(BaseClass));
            Assert.AreEqual("CREATE TABLE [BaseClass] ([ObjectId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, [_dscr] INT NOT NULL, [Prop1] INT NOT NULL);", result);            
        }


        [Test]
        public void TestSimpleQueryWithDerived()
        {
            const string path = "Prop1";
            const string derivedType = "DerivedClass";
            var types = FluentConfiguration.Start()
                .DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName("_dscr")
                .AddType<BaseClass>(z => z.AllProperties())
                .AddType<DerivedClass>(z => z.AllProperties())
                .GenerateTypeMappings();
            var provider = new TestProvider(types);         
            var queryBuilder = new QueryBuilder(provider);
            var plan = queryBuilder.GetQuery(derivedType, new[]
            {
                path
            });
            Assert.AreEqual("SELECT [M1].*, [T1].* FROM [BaseClass] AS [M1] INNER JOIN [DerivedClass] AS [T1] ON [M1].[ObjectId] = [T1].[ObjectId] WHERE [M1].[Prop1] = @p0", plan.SqlString);
            Assert.AreEqual(1, plan.SelectClause.Parts.OfType<TypePart>().Count());
            Assert.IsTrue(plan.SelectClause.Parts.OfType<TypePart>().All(z => z.Tables.Count == 2));
        }


        [Test]
        public void TestQueryNestedClassOneToOne()
        {
            var types = FluentConfiguration.Start()
                .DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName("_dscr")
                .AddType<BaseWithNavigationClass>(z => z.AllProperties())
                .AddType<NavigationedClass>(z => z.AllProperties())
                .GenerateTypeMappings();
            var provider = new TestProvider(types);     
            var queryBuilder = new QueryBuilder(provider);

            var plan = queryBuilder.GetQuery("BaseWithNavigationClass", new[]
            {
                "Nav.Something"
            });
            Assert.AreEqual("SELECT [M1].* FROM [BaseWithNavigationClass] AS [M1] INNER JOIN [NavigationedClass] AS [T1] ON [M1].[Nav] = [T1].[ObjectId] WHERE [T1].[Something] = @p0", plan.SqlString);
        }

        [Test]
        public void TestQueryNestedClassInclude()
        {
            var types = FluentConfiguration.Start()
                .DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName("_dscr")
                .AddType<BaseWithNavigationClass>(z => z.AllProperties())
                .AddType<NavigationedClass>(z => z.AllProperties())
                .GenerateTypeMappings();
            var provider = new TestProvider(types);
            var queryBuilder = new QueryBuilder(provider);

            var plan = queryBuilder.GetQuery("BaseWithNavigationClass", new[]
            {
                "Nav.Something"
            }, new[] { "Nav" });
            Assert.AreEqual("SELECT [M1].*, [S1].* FROM [BaseWithNavigationClass] AS [M1] LEFT OUTER JOIN [NavigationedClass] AS [S1] ON [M1].[Nav] = [S1].[ObjectId] INNER JOIN [NavigationedClass] AS [T1] ON [M1].[Nav] = [T1].[ObjectId] WHERE [T1].[Something] = @p0", plan.SqlString);
            Assert.AreEqual(2, plan.SelectClause.Parts.OfType<TypePart>().Count());
            Assert.AreEqual(1, plan.SelectClause.Parts.OfType<SubTypePart>().Count());
            Assert.IsTrue(plan.SelectClause.Parts.OfType<TypePart>().All(z => z.Tables.Count == 1));
            Assert.IsTrue(plan.SelectClause.Parts.OfType<SubTypePart>().All(z => z.CollectingProperty.Name == "Nav"));
        }
    }
}