using System.Linq;
using NUnit.Framework;
using PTORMPrototype.Mapping.Configuration;
using PTORMPrototype.Query;
using PTORTMTests.TestClasses;
using PTORTMTests.TestClasses.Collections;
using PTORTMTests.TestClasses.Inheritance;

namespace PTORTMTests
{
    [TestFixture]
    public class QueryBuilderTest
    {
        private const string IdentityField = "ObjectId";
        private const string DefaultDiscriminator = "_dscr";

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
        public void TestQueryWithCollectionInInclude()
        {
            var types = FluentConfiguration.Start().DefaultIdProperty(IdentityField)
                .AddTypeAuto<ClassWithCollection>()
                .AddTypeAuto<CollectionItem>()
                .GenerateTypeMappings();
            var provider = new TestProvider(types);
            var queryBuilder = new QueryBuilder(provider);
            var plan = queryBuilder.GetQuery("ClassWithCollection", new string[0], new[]
            {
                "Collection"
            });
            Assert.AreEqual("SELECT [M1].*, [S1].* FROM [ClassWithCollection] AS [M1] LEFT OUTER JOIN [CollectionItem] AS [S1] ON [M1].[ObjectId] = [S1].[ClassWithCollection_Collection]", plan.SqlString);
            Assert.AreEqual(2, plan.SelectClause.Parts.OfType<TypePart>().Count());
            Assert.IsTrue(plan.SelectClause.Parts.OfType<TypePart>().All(z => z.Tables.Count == 1));
        }

        [Test]
        public void TestQueryWithCollectionInFilter()
        {
            var types = FluentConfiguration.Start().DefaultIdProperty(IdentityField)
                .AddTypeAuto<ClassWithCollection>()
                .AddTypeAuto<CollectionItem>()
                .GenerateTypeMappings();
            var provider = new TestProvider(types);
            var queryBuilder = new QueryBuilder(provider);
            var plan = queryBuilder.GetQuery("ClassWithCollection", new[]
            {
                "Collection.Property"
            });
            Assert.AreEqual("SELECT [M1].* FROM [ClassWithCollection] AS [M1] INNER JOIN [CollectionItem] AS [M1B0T1] ON [M1].[ObjectId] = [M1B0T1].[ClassWithCollection_Collection] WHERE [M1B0T1].[Property] = @p0", plan.SqlString);
        }

        [Test]
        public void TestQueryWithArrayFilter()
        {
            var types = FluentConfiguration.Start().DefaultIdProperty(IdentityField)
                .AddTypeAuto<ClassWithIntArr>()                
                .GenerateTypeMappings();
            var provider = new TestProvider(types);
            var queryBuilder = new QueryBuilder(provider);
            var plan = queryBuilder.GetQuery("ClassWithIntArr", new[]
            {
                "Arr"
            });
            Assert.AreEqual("SELECT [M1].* FROM [ClassWithIntArr] AS [M1] INNER JOIN [ClassWithIntArr_Arr] AS [T1] ON [M1].[ObjectId] = [T1].[ParentId] WHERE [T1].[Value] = @p0", plan.SqlString);
        }


        [Test]
        public void TestQueryWithArrayInInclude()
        {
            var types = FluentConfiguration.Start().DefaultIdProperty(IdentityField)
                .AddTypeAuto<ClassWithIntArr>()

                .GenerateTypeMappings();
            var provider = new TestProvider(types);
            var queryBuilder = new QueryBuilder(provider);
            var plan = queryBuilder.GetQuery("ClassWithIntArr", new string[0], new[]
            {
                "Arr"
            });
            Assert.AreEqual("SELECT [M1].*, [S1].* FROM [ClassWithIntArr] AS [M1] LEFT OUTER JOIN [ClassWithIntArr_Arr] AS [S1] ON [M1].[ObjectId] = [S1].[ParentId]", plan.SqlString);
            Assert.AreEqual(2, plan.SelectClause.Parts.Count());
            Assert.IsTrue(plan.SelectClause.Parts.OfType<TypePart>().All(z => z.Tables.Count == 1));
        }


        [Test]
        public void TestCreateTableQuery()
        {        
            var types = FluentConfiguration.Start().DefaultIdProperty(IdentityField).DefaultDiscriminatorColumnName(DefaultDiscriminator).AddType<BaseClass>(z => z.AllProperties()).GenerateTypeMappings();
            var provider = new TestProvider(types);
            var queryBuilder = new QueryBuilder(provider);
            var result = queryBuilder.GetCreateTable(typeof(BaseClass));
            Assert.AreEqual("CREATE TABLE [BaseClass] ([ObjectId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, [_dscr] INT NOT NULL, [Prop1] INT NOT NULL);", result);            
        }

        [Test]
        public void TestCreateTableWithArray()
        {
            var types = FluentConfiguration.Start().DefaultIdProperty(IdentityField).DefaultDiscriminatorColumnName(DefaultDiscriminator)
                .AddType<ClassWithIntArr>(z => z.AllProperties()).GenerateTypeMappings();
            var provider = new TestProvider(types);
            var queryBuilder = new QueryBuilder(provider);
            var result = queryBuilder.GetCreateTable(typeof(ClassWithIntArr));
            Assert.AreEqual("CREATE TABLE [ClassWithIntArr] ([ObjectId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, [_dscr] INT NOT NULL);\n"
                +"CREATE TABLE [ClassWithIntArr_Arr] ([ParentId] UNIQUEIDENTIFIER NOT NULL, [Value] INT NOT NULL, [Index] BIGINT NOT NULL);", result);
        }


        [Test]
        public void TestSimpleQueryWithDerived()
        {
            const string path = "Prop1";
            const string derivedType = "DerivedClass";
            var types = FluentConfiguration.Start()
                .DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName(DefaultDiscriminator)
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
        public void TestSimpleQueryWithDerivedProperty()
        {
            const string path = "Prop2";
            const string derivedType = "DerivedClass";
            var types = FluentConfiguration.Start()
                .DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName(DefaultDiscriminator)
                .AddType<BaseClass>(z => z.AllProperties())
                .AddType<DerivedClass>(z => z.AllProperties())
                .GenerateTypeMappings();
            var provider = new TestProvider(types);
            var queryBuilder = new QueryBuilder(provider);
            var plan = queryBuilder.GetQuery(derivedType, new[]
            {
                path
            });
            Assert.AreEqual("SELECT [M1].*, [T1].* FROM [BaseClass] AS [M1] INNER JOIN [DerivedClass] AS [T1] ON [M1].[ObjectId] = [T1].[ObjectId] WHERE [T1].[Prop2] = @p0", plan.SqlString);
            Assert.AreEqual(1, plan.SelectClause.Parts.OfType<TypePart>().Count());
            Assert.IsTrue(plan.SelectClause.Parts.OfType<TypePart>().All(z => z.Tables.Count == 2));
        }

        [Test]
        public void TestQueryWithDerivedChildProperty()
        {            
            var types = FluentConfiguration.Start()
                .DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName(DefaultDiscriminator)
                .AddType<Parent>(z => z.AllProperties())
                .AddType<Child>(z => z.AllProperties())
                .AddType<ChildDerived>(z => z.AllProperties())
                .GenerateTypeMappings();
            var provider = new TestProvider(types);
            var queryBuilder = new QueryBuilder(provider);
            var plan = queryBuilder.GetQuery("Parent", new[]
            {
                "Child.DerivedProperty"
            });
            Assert.AreEqual("SELECT [M1].* FROM [Parent] AS [M1] INNER JOIN [Child] AS [M1B0T1] ON [M1].[Child] = [M1B0T1].[ObjectId] INNER JOIN [ChildDerived] AS [M1B0T1B1T1] ON [M1B0T1].[ObjectId] = [M1B0T1B1T1].[ObjectId] WHERE [M1B0T1B1T1].[DerivedProperty] = @p0", plan.SqlString);
            Assert.AreEqual(1, plan.SelectClause.Parts.OfType<TypePart>().Count());
            Assert.IsTrue(plan.SelectClause.Parts.OfType<TypePart>().All(z => z.Tables.Count == 1));
        }

        [Test]
        public void TestQueryWithDerivedChildPropertyConcreteType()
        {
            var types = FluentConfiguration.Start()
                .DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName(DefaultDiscriminator)
                .AddType<Parent>(z => z.AllProperties())
                .AddType<Child>(z => z.AllProperties())
                .AddType<ChildDerived>(z => z.AllProperties())
                .GenerateTypeMappings();
            var provider = new TestProvider(types);
            var queryBuilder = new QueryBuilder(provider);
            var plan = queryBuilder.GetQuery("Parent", new[]
            {
                "Child[ChildDerived].DerivedProperty"
            });
            Assert.AreEqual("SELECT [M1].* FROM [Parent] AS [M1] INNER JOIN [Child] AS [M1B0T1] ON [M1].[Child] = [M1B0T1].[ObjectId] INNER JOIN [ChildDerived] AS [M1B0T1B1T1] ON [M1B0T1].[ObjectId] = [M1B0T1B1T1].[ObjectId] WHERE [M1B0T1B1T1].[DerivedProperty] = @p0", plan.SqlString);
            Assert.AreEqual(1, plan.SelectClause.Parts.OfType<TypePart>().Count());
            Assert.IsTrue(plan.SelectClause.Parts.OfType<TypePart>().All(z => z.Tables.Count == 1));
        }

        [Test]
        public void TestQueryNestedClassOneToOne()
        {
            var types = FluentConfiguration.Start()
                .DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName(DefaultDiscriminator)
                .AddType<BaseWithNavigationClass>(z => z.AllProperties())
                .AddType<NavigationedClass>(z => z.AllProperties())
                .GenerateTypeMappings();
            var provider = new TestProvider(types);     
            var queryBuilder = new QueryBuilder(provider);

            var plan = queryBuilder.GetQuery("BaseWithNavigationClass", new[]
            {
                "Nav.Something"
            });
            Assert.AreEqual("SELECT [M1].* FROM [BaseWithNavigationClass] AS [M1] INNER JOIN [NavigationedClass] AS [M1B0T1] ON [M1].[Nav] = [M1B0T1].[ObjectId] WHERE [M1B0T1].[Something] = @p0", plan.SqlString);
        }

        [Test]
        public void TestQueryNestedClassInclude()
        {
            var types = FluentConfiguration.Start()
                .DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName(DefaultDiscriminator)
                .AddType<BaseWithNavigationClass>(z => z.AllProperties())
                .AddType<NavigationedClass>(z => z.AllProperties())
                .GenerateTypeMappings();
            var provider = new TestProvider(types);
            var queryBuilder = new QueryBuilder(provider);

            var plan = queryBuilder.GetQuery("BaseWithNavigationClass", new[]
            {
                "Nav.Something"
            }, new[] { "Nav" });
            Assert.AreEqual("SELECT [M1].*, [S1].* FROM [BaseWithNavigationClass] AS [M1] LEFT OUTER JOIN [NavigationedClass] AS [S1] ON [M1].[Nav] = [S1].[ObjectId] INNER JOIN [NavigationedClass] AS [M1B0T1] ON [M1].[Nav] = [M1B0T1].[ObjectId] WHERE [M1B0T1].[Something] = @p0", plan.SqlString);
            Assert.AreEqual(2, plan.SelectClause.Parts.OfType<TypePart>().Count());
            Assert.AreEqual(1, plan.SelectClause.Parts.OfType<SubTypePart>().Count());
            Assert.IsTrue(plan.SelectClause.Parts.OfType<TypePart>().All(z => z.Tables.Count == 1));
            Assert.IsTrue(plan.SelectClause.Parts.OfType<SubTypePart>().All(z => z.CollectingProperty.Name == "Nav"));
        }


        [Test]
        public void TestIncludeWithInheritance()
        {
            var types = FluentConfiguration.Start()
                .DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName(DefaultDiscriminator)
                .AddTypeAuto<Parent>()
                .AddTypeAuto<Child>()
                .AddTypeAuto<ChildDerived>()                
                .GenerateTypeMappings();
            var provider = new TestProvider(types);
            var queryBuilder = new QueryBuilder(provider);

            var plan = queryBuilder.GetQuery("Parent", new string[0],  new[]
            {
                "Child[ChildDerived]"
            });
            Assert.AreEqual("SELECT [M1].*, [S1].*, [S2].* FROM [Parent] AS [M1] LEFT OUTER JOIN [Child] AS [S1] ON [M1].[Child] = [S1].[ObjectId] INNER JOIN [ChildDerived] AS [S2] ON [S1].[ObjectId] = [S2].[ObjectId]", plan.SqlString);
            Assert.AreEqual(2, plan.SelectClause.Parts.OfType<TypePart>().Count());
            Assert.AreEqual(1, plan.SelectClause.Parts.OfType<SubTypePart>().Count());            
            Assert.IsTrue(plan.SelectClause.Parts.OfType<SubTypePart>().All(z => z.Tables.Count == 2));
        }

        [Test]
        public void UpdateSimpleProperties()
        {
            const string type = "BaseClass";
            const string path = "Prop1";
            var types = FluentConfiguration.Start().DefaultIdProperty(IdentityField).AddType<BaseClass>(z => z.AllProperties()).GenerateTypeMappings();
            var provider = new TestProvider(types);
            var queryBuilder = new QueryBuilder(provider);
            var plan = queryBuilder.GetUpdate(type, new [] { path });
            Assert.AreEqual(1, plan.Parts.Count());
            Assert.AreEqual("UPDATE [M1] SET [M1].[Prop1] = @p1 FROM [BaseClass] AS [M1] WHERE [M1].[ObjectId] = @p0", plan.Parts.First().SqlString);
        }

        [Test]
        public void UpdateDerivedroperties()
        {
            const string type = "DerivedClass";            
            var types = FluentConfiguration.Start().DefaultIdProperty(IdentityField)
                .AddTypeAuto<BaseClass>()
                .AddTypeAuto<DerivedClass>()
                .GenerateTypeMappings();
            var provider = new TestProvider(types);
            var queryBuilder = new QueryBuilder(provider);
            var plan = queryBuilder.GetUpdate(type, new [] { "Prop1", "Prop2" });
            Assert.AreEqual(2, plan.Parts.Count());
            Assert.AreEqual("UPDATE [M1] SET [M1].[Prop1] = @p1 FROM [BaseClass] AS [M1] WHERE [M1].[ObjectId] = @p0", plan.Parts.First().SqlString);
            Assert.AreEqual("UPDATE [M1] SET [M1].[Prop2] = @p1 FROM [DerivedClass] AS [M1] WHERE [M1].[ObjectId] = @p0", plan.Parts.Last().SqlString);
        }

        [Test]
        public void InsertSimpleProperties()
        {
            const string type = "BaseClass";
            var types = FluentConfiguration.Start().DefaultIdProperty(IdentityField).
                DefaultDiscriminatorColumnName(DefaultDiscriminator).AddType<BaseClass>(z => z.AllProperties()).GenerateTypeMappings();
            var provider = new TestProvider(types);
            var queryBuilder = new QueryBuilder(provider);
            var plan = queryBuilder.GetInsert(type);
            Assert.AreEqual(1, plan.Parts.Count());
            Assert.AreEqual("INSERT INTO [BaseClass] ([_dscr], [ObjectId], [Prop1]) VALUES(@p0, @p1, @p2)", plan.Parts.First().SqlString);
        }

        [Test]
        public void InsertDerivedProperties()
        {
            const string type = "DerivedClass";            
            var types = FluentConfiguration.Start().DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName(DefaultDiscriminator)
               .AddTypeAuto<BaseClass>()
               .AddTypeAuto<DerivedClass>()
               .GenerateTypeMappings();
            var provider = new TestProvider(types);
            var queryBuilder = new QueryBuilder(provider);
            var plan = queryBuilder.GetInsert(type);
            Assert.AreEqual(2, plan.Parts.Count());
            Assert.AreEqual("INSERT INTO [BaseClass] ([_dscr], [ObjectId], [Prop1]) VALUES(@p0, @p1, @p2)", plan.Parts.First().SqlString);
            Assert.AreEqual("INSERT INTO [DerivedClass] ([ObjectId], [Prop2]) VALUES(@p0, @p1)", plan.Parts.Last().SqlString);
        }

        [Test]
        public void InsertPrimitiveListProperties()
        {
            const string type = "ClassWithIntArr";
            var types = FluentConfiguration.Start().DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName(DefaultDiscriminator)
               .AddTypeAuto<ClassWithIntArr>()               
               .GenerateTypeMappings();
            var provider = new TestProvider(types);
            var queryBuilder = new QueryBuilder(provider);
            var plan = queryBuilder.GetInsert(type);
            Assert.AreEqual(2, plan.Parts.Count());
            Assert.AreEqual("INSERT INTO [ClassWithIntArr] ([_dscr], [ObjectId]) VALUES(@p0, @p1)", plan.Parts.First().SqlString);
            Assert.AreEqual("INSERT INTO [ClassWithIntArr_Arr] ([ParentId], [Value], [Index]) VALUES(@p0, @p1, @p2)", plan.Parts.Last().SqlString);
        }

        [Test]
        public void UpdatePrimitiveListProperties()
        {
            const string type = "ClassWithIntArr";
            var types = FluentConfiguration.Start().DefaultIdProperty(IdentityField)
                  .DefaultDiscriminatorColumnName(DefaultDiscriminator)
                 .AddTypeAuto<ClassWithIntArr>()
                 .GenerateTypeMappings();
            var provider = new TestProvider(types);
            var queryBuilder = new QueryBuilder(provider);
            var plan = queryBuilder.GetUpdate(type, new[] { "Arr" });
            Assert.AreEqual(2, plan.Parts.Count());
            Assert.AreEqual("DELETE FROM [M1] FROM [ClassWithIntArr_Arr] AS [M1] WHERE [M1].[ParentId] = @p0", plan.Parts.First().SqlString);
            Assert.AreEqual("INSERT INTO [ClassWithIntArr_Arr] ([ParentId], [Value], [Index]) VALUES(@p0, @p1, @p2)", plan.Parts.Last().SqlString);
        }
    }
}