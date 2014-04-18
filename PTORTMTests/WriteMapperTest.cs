using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NUnit.Framework;
using PTORMPrototype.Mapping;
using PTORMPrototype.Mapping.Configuration;
using PTORMPrototype.Query;
using PTORTMTests.TestClasses;
using PTORTMTests.TestClasses.Collections;
using StructureMap.AutoMocking;

namespace PTORTMTests
{
    [TestFixture]
    public class WriteMapperTest : BaseSqlTest
    {
        private const string IdentityField = "ObjectId";
        private const string DefaultDiscriminator = "_dscr";
        private RhinoAutoMocker<WriteMapper> _autoMocker;
        private IEnumerable<TypeMappingInfo> _types;
        private List<string> _tables;
            
        [SetUp]
        public void Setup()
        {
            _tables = new List<string>();
            _autoMocker = new RhinoAutoMocker<WriteMapper>();   
            _autoMocker.Container.Configure(z =>
            {                
                z.For<IMetaInfoProvider>().Singleton().Use(() => new TestProvider(_types));
                z.For<QueryBuilder>().Singleton().Use<QueryBuilder>();
            });
        }

        [TearDown]
        public void TearDown()
        {
            using (var cmd1 = SqlConnection.CreateCommand())
            {
                cmd1.CommandText = string.Concat(_tables.Select(t => string.Format((string) "DROP TABLE {0};", (object) t)));
                cmd1.ExecuteNonQuery();
            }
        }

        private void CreateTable<T>()
        {
            using (var cmd1 = SqlConnection.CreateCommand())
            {
                _tables.Add(typeof(T).Name);
                cmd1.CommandText = _autoMocker.Get<QueryBuilder>().GetCreateTable(typeof (T));
                cmd1.ExecuteNonQuery();
            }
        }

        [Test]
        public void TestInsertSimple()
        {
            //Arrange
            _types = FluentConfiguration.Start().DefaultIdProperty(IdentityField).
                DefaultDiscriminatorColumnName(DefaultDiscriminator).AddType<BaseClass>(z => z.AllProperties()).GenerateTypeMappings().ToList();
            CreateTable<BaseClass>();
            var objectId = Guid.NewGuid();
            const int prop1 = 5;
            var newObject = new BaseClass { ObjectId = objectId, Prop1 = prop1};
            //Act            
            _autoMocker.ClassUnderTest.InsertObject(SqlConnection, newObject);
            //Assert
            using (var cmd = SqlConnection.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM [BaseClass]";
                using (var reader = cmd.ExecuteReader())
                {
                    Assert.IsTrue(reader.Read(), "Нет записей");
                    Assert.AreEqual(objectId, reader.GetGuid(reader.GetOrdinal(IdentityField)));
                    Assert.AreEqual(1, reader.GetInt32(reader.GetOrdinal(DefaultDiscriminator)));
                    Assert.AreEqual(prop1, reader.GetInt32(reader.GetOrdinal("Prop1")));
                    Assert.IsFalse(reader.Read(), "Слишком много записей");
                }
            }
        }

        [Test]
        public void TestInsertDerived()
        {
            //Arrange
            _types = FluentConfiguration.Start().DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName(DefaultDiscriminator)
                .AddTypeAuto<BaseClass>()
                .AddTypeAuto<DerivedClass>()
                .GenerateTypeMappings().ToList();
            CreateTable<BaseClass>();
            CreateTable<DerivedClass>();
            var objectId = Guid.NewGuid();
            const int prop1 = 5;
            const int prop2 = 7;
            var newObject = new DerivedClass { ObjectId = objectId, Prop1 = prop1, Prop2 = prop2};
            //Act            
            _autoMocker.ClassUnderTest.InsertObject(SqlConnection, newObject);
            //Assert
            using (var cmd = SqlConnection.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM [BaseClass]";
                using (var reader = cmd.ExecuteReader())
                {
                    Assert.IsTrue(reader.Read(), "Нет записей");
                    Assert.AreEqual(objectId, reader.GetGuid(reader.GetOrdinal(IdentityField)));
                    Assert.AreEqual(2, reader.GetInt32(reader.GetOrdinal(DefaultDiscriminator)));
                    Assert.AreEqual(prop1, reader.GetInt32(reader.GetOrdinal("Prop1")));
                    Assert.IsFalse(reader.Read(), "Слишком много записей");
                }                

                cmd.CommandText = "SELECT * FROM [DerivedClass]";
                using (var reader = cmd.ExecuteReader())
                {
                    Assert.IsTrue(reader.Read(), "Нет записей");
                    Assert.AreEqual(objectId, reader.GetGuid(reader.GetOrdinal(IdentityField)));                    
                    Assert.AreEqual(prop2, reader.GetInt32(reader.GetOrdinal("Prop2")));
                    Assert.IsFalse(reader.Read(), "Слишком много записей");
                }
            }
        }

        [Test]
        public void TestInsertNavigation()
        {
            //Arrange
            _types = FluentConfiguration.Start().DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName(DefaultDiscriminator)
                .AddTypeAuto<BaseWithNavigationClass>()
                .AddTypeAuto<NavigationedClass>()
                .GenerateTypeMappings().ToList();
            CreateTable<BaseWithNavigationClass>();
            CreateTable<NavigationedClass>();
            var objectId = Guid.NewGuid();
            var navId = Guid.NewGuid();
            const string something = "som";
            var newObject = new BaseWithNavigationClass { ObjectId = objectId, Nav = new NavigationedClass { ObjectId = navId, Something = something } };
            //Act            
            _autoMocker.ClassUnderTest.InsertObject(SqlConnection, newObject);
            //Assert
            using (var cmd = SqlConnection.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM [BaseWithNavigationClass]";
                using (var reader = cmd.ExecuteReader())
                {
                    Assert.IsTrue(reader.Read(), "Нет записей");
                    Assert.AreEqual(objectId, reader.GetGuid(reader.GetOrdinal(IdentityField)));
                    Assert.AreEqual(1, reader.GetInt32(reader.GetOrdinal(DefaultDiscriminator)));
                    Assert.AreEqual(navId, reader.GetGuid(reader.GetOrdinal("Nav")));
                    Assert.IsFalse(reader.Read(), "Слишком много записей");
                }

                cmd.CommandText = "SELECT * FROM [NavigationedClass]";
                using (var reader = cmd.ExecuteReader())
                {
                    Assert.IsTrue(reader.Read(), "Нет записей");
                    Assert.AreEqual(navId, reader.GetGuid(reader.GetOrdinal(IdentityField)));
                    Assert.AreEqual(1, reader.GetInt32(reader.GetOrdinal(DefaultDiscriminator)));
                    Assert.AreEqual(something, reader.GetString(reader.GetOrdinal("Something")));
                    Assert.IsFalse(reader.Read(), "Слишком много записей");
                }
            }
        }

        [Test]
        public void TestInsertNavigationCollection()
        {
            //Arrange
            _types = FluentConfiguration.Start().DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName(DefaultDiscriminator)
                .AddTypeAuto<ClassWithCollection>()
                .AddTypeAuto<CollectionItem>()
                .GenerateTypeMappings().ToList();
            CreateTable<ClassWithCollection>();
            CreateTable<CollectionItem>();
            var objectId = Guid.NewGuid();
            var nav1Id = Guid.NewGuid();
            var nav2Id = Guid.NewGuid();
            var newObject = new ClassWithCollection 
            { 
                ObjectId = objectId, 
                Collection = new Collection<CollectionItem>
                {
                    new CollectionItem { ObjectId = nav1Id, Property = "1" },
                    new CollectionItem { ObjectId = nav2Id, Property = "2" }
                }
            };
            //Act            
            _autoMocker.ClassUnderTest.InsertObject(SqlConnection, newObject);
            //Assert
            using (var cmd = SqlConnection.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM [ClassWithCollection]";
                using (var reader = cmd.ExecuteReader())
                {
                    Assert.IsTrue(reader.Read(), "Нет записей");
                    Assert.AreEqual(objectId, reader.GetGuid(reader.GetOrdinal(IdentityField)));
                    Assert.AreEqual(1, reader.GetInt32(reader.GetOrdinal(DefaultDiscriminator)));                    
                    Assert.IsFalse(reader.Read(), "Слишком много записей");
                }

                cmd.CommandText = "SELECT * FROM [CollectionItem] ORDER BY [Property]";
                using (var reader = cmd.ExecuteReader())
                {
                    Assert.IsTrue(reader.Read(), "Нет записей");
                    Assert.AreEqual(nav1Id, reader.GetGuid(reader.GetOrdinal(IdentityField)));
                    Assert.AreEqual(1, reader.GetInt32(reader.GetOrdinal(DefaultDiscriminator)));
                    Assert.AreEqual("1", reader.GetString(reader.GetOrdinal("Property")));
                    Assert.AreEqual(objectId, reader.GetGuid(reader.GetOrdinal("ClassWithCollection_Collection")));
                    Assert.IsTrue(reader.Read(), "Нет записи №2");
                    Assert.AreEqual(nav2Id, reader.GetGuid(reader.GetOrdinal(IdentityField)));
                    Assert.AreEqual(1, reader.GetInt32(reader.GetOrdinal(DefaultDiscriminator)));
                    Assert.AreEqual("2", reader.GetString(reader.GetOrdinal("Property")));
                    Assert.AreEqual(objectId, reader.GetGuid(reader.GetOrdinal("ClassWithCollection_Collection")));
                    Assert.IsFalse(reader.Read(), "Слишком много записей");
                }
            }
        }


        [Test]
        public void TestInsertPrimitiveCollection()
        {
            //Arrange
            _types = FluentConfiguration.Start().DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName(DefaultDiscriminator)
                .AddTypeAuto<ClassWithIntArr>()                
                .GenerateTypeMappings().ToList();
            CreateTable<ClassWithIntArr>();
            _tables.Add("ClassWithIntArr_Arr");
            var objectId = Guid.NewGuid();            
            var newObject = new ClassWithIntArr
            {
                ObjectId = objectId,
                Arr = new []{ 1, 2 }
            };
            //Act            
            _autoMocker.ClassUnderTest.InsertObject(SqlConnection, newObject);
            //Assert
            using (var cmd = SqlConnection.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM [ClassWithIntArr]";
                using (var reader = cmd.ExecuteReader())
                {
                    Assert.IsTrue(reader.Read(), "Нет записей");
                    Assert.AreEqual(objectId, reader.GetGuid(reader.GetOrdinal(IdentityField)));
                    Assert.AreEqual(1, reader.GetInt32(reader.GetOrdinal(DefaultDiscriminator)));
                    Assert.IsFalse(reader.Read(), "Слишком много записей");
                }

                cmd.CommandText = "SELECT * FROM [ClassWithIntArr_Arr] ORDER BY [Index]";
                using (var reader = cmd.ExecuteReader())
                {
                    Assert.IsTrue(reader.Read(), "Нет записей");                    
                    Assert.AreEqual(1, reader.GetInt32(reader.GetOrdinal("Value")));
                    Assert.AreEqual(0L, reader.GetInt64(reader.GetOrdinal("Index")));                    
                    Assert.AreEqual(objectId, reader.GetGuid(reader.GetOrdinal("ParentId")));
                    Assert.IsTrue(reader.Read(), "Нет записи №2");
                    Assert.AreEqual(2, reader.GetInt32(reader.GetOrdinal("Value")));
                    Assert.AreEqual(1L, reader.GetInt64(reader.GetOrdinal("Index")));
                    Assert.AreEqual(objectId, reader.GetGuid(reader.GetOrdinal("ParentId")));
                    Assert.IsFalse(reader.Read(), "Слишком много записей");
                }
            }
        }

        [Test]
        public void TestUpdateSimple()
        {
            //Arrange
            _types = FluentConfiguration.Start().DefaultIdProperty(IdentityField).
                DefaultDiscriminatorColumnName(DefaultDiscriminator).AddType<BaseClass>(z => z.AllProperties()).GenerateTypeMappings().ToList();
            CreateTable<BaseClass>();
            var objectId = Guid.NewGuid();
            const int prop1 = 5;
            var newObject = new BaseClass { ObjectId = objectId, Prop1 = prop1 };
            using (var inserCmd = SqlConnection.CreateCommand())
            {
                inserCmd.CommandText = "INSERT INTO [BaseClass] ([ObjectId], [_dscr], [Prop1]) VALUES(@P1, @P2, @P3)";
                inserCmd.Parameters.AddWithValue("@P1", objectId);
                inserCmd.Parameters.AddWithValue("@P2", 1);
                inserCmd.Parameters.AddWithValue("@P3", 4);
                inserCmd.ExecuteNonQuery();
            }

            //Act            
            _autoMocker.ClassUnderTest.UpdateObject(SqlConnection, newObject, new [] {"Prop1"});
            //Assert
            using (var cmd = SqlConnection.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM [BaseClass]";
                using (var reader = cmd.ExecuteReader())
                {
                    Assert.IsTrue(reader.Read(), "Нет записей");
                    Assert.AreEqual(objectId, reader.GetGuid(reader.GetOrdinal(IdentityField)));
                    Assert.AreEqual(1, reader.GetInt32(reader.GetOrdinal(DefaultDiscriminator)));
                    Assert.AreEqual(prop1, reader.GetInt32(reader.GetOrdinal("Prop1")));
                    Assert.IsFalse(reader.Read(), "Слишком много записей");
                }
            }
        }

        [Test]
        public void TestUpdateDerived()
        {
            //Arrange
            _types = FluentConfiguration.Start().DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName(DefaultDiscriminator)
                .AddTypeAuto<BaseClass>()
                .AddTypeAuto<DerivedClass>()
                .GenerateTypeMappings().ToList();
            CreateTable<BaseClass>();
            CreateTable<DerivedClass>();
            var objectId = Guid.NewGuid();
            const int prop1 = 5;
            const int prop2 = 7;
            var newObject = new DerivedClass { ObjectId = objectId, Prop1 = prop1, Prop2 = prop2 };
            using (var inserCmd = SqlConnection.CreateCommand())
            {
                inserCmd.CommandText = "INSERT INTO [BaseClass] ([ObjectId], [_dscr], [Prop1]) VALUES(@P1, @P2, @P3)";
                inserCmd.Parameters.AddWithValue("@P1", objectId);
                inserCmd.Parameters.AddWithValue("@P2", 2);
                inserCmd.Parameters.AddWithValue("@P3", 4);
                inserCmd.ExecuteNonQuery();
                inserCmd.Parameters.Clear();                
                inserCmd.CommandText = "INSERT INTO [DerivedClass] ([ObjectId], [Prop2]) VALUES(@P1, @P2)";
                inserCmd.Parameters.AddWithValue("@P1", objectId);
                inserCmd.Parameters.AddWithValue("@P2", 6);
                inserCmd.ExecuteNonQuery();
            }

            //Act            
            _autoMocker.ClassUnderTest.UpdateObject(SqlConnection, newObject, new[] { "Prop1", "Prop2" });
            //Assert
            using (var cmd = SqlConnection.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM [BaseClass]";
                using (var reader = cmd.ExecuteReader())
                {
                    Assert.IsTrue(reader.Read(), "Нет записей");
                    Assert.AreEqual(objectId, reader.GetGuid(reader.GetOrdinal(IdentityField)));
                    Assert.AreEqual(2, reader.GetInt32(reader.GetOrdinal(DefaultDiscriminator)));
                    Assert.AreEqual(prop1, reader.GetInt32(reader.GetOrdinal("Prop1")));
                    Assert.IsFalse(reader.Read(), "Слишком много записей");
                }

                cmd.CommandText = "SELECT * FROM [DerivedClass]";
                using (var reader = cmd.ExecuteReader())
                {
                    Assert.IsTrue(reader.Read(), "Нет записей");
                    Assert.AreEqual(objectId, reader.GetGuid(reader.GetOrdinal(IdentityField)));
                    Assert.AreEqual(prop2, reader.GetInt32(reader.GetOrdinal("Prop2")));
                    Assert.IsFalse(reader.Read(), "Слишком много записей");
                }
            }
        }


        [Test]
        public void TestUpdatePrimitiveCollection()
        {
            //Arrange
            _types = FluentConfiguration.Start().DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName(DefaultDiscriminator)
                .AddTypeAuto<ClassWithIntArr>()
                .GenerateTypeMappings().ToList();
            CreateTable<ClassWithIntArr>();
            _tables.Add("ClassWithIntArr_Arr");
            var objectId = Guid.NewGuid();
            var newObject = new ClassWithIntArr
            {
                ObjectId = objectId,
                Arr = new[] { 1, 2 }
            };
            using (var inserCmd = SqlConnection.CreateCommand())
            {
                inserCmd.CommandText = "INSERT INTO [ClassWithIntArr] ([ObjectId], [_dscr]) VALUES(@P1, @P2)";
                inserCmd.Parameters.AddWithValue("@P1", objectId);
                inserCmd.Parameters.AddWithValue("@P2", 1);
                inserCmd.ExecuteNonQuery();
                inserCmd.Parameters.Clear();
                inserCmd.CommandText = "INSERT INTO [ClassWithIntArr_Arr] ([ParentId], [Value], [Index]) VALUES(@P0, @P1, @P2)";
                inserCmd.Parameters.AddWithValue("@P0", objectId);
                inserCmd.Parameters.AddWithValue("@P1", 3);
                inserCmd.Parameters.AddWithValue("@P2", 0);
                inserCmd.ExecuteNonQuery();
            }

            //Act            
            _autoMocker.ClassUnderTest.UpdateObject(SqlConnection, newObject, new [] { "Arr" });
            //Assert
            using (var cmd = SqlConnection.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM [ClassWithIntArr]";
                using (var reader = cmd.ExecuteReader())
                {
                    Assert.IsTrue(reader.Read(), "Нет записей");
                    Assert.AreEqual(objectId, reader.GetGuid(reader.GetOrdinal(IdentityField)));
                    Assert.AreEqual(1, reader.GetInt32(reader.GetOrdinal(DefaultDiscriminator)));
                    Assert.IsFalse(reader.Read(), "Слишком много записей");
                }

                cmd.CommandText = "SELECT * FROM [ClassWithIntArr_Arr] ORDER BY [Index]";
                using (var reader = cmd.ExecuteReader())
                {
                    Assert.IsTrue(reader.Read(), "Нет записей");
                    Assert.AreEqual(1, reader.GetInt32(reader.GetOrdinal("Value")));
                    Assert.AreEqual(0L, reader.GetInt64(reader.GetOrdinal("Index")));
                    Assert.AreEqual(objectId, reader.GetGuid(reader.GetOrdinal("ParentId")));
                    Assert.IsTrue(reader.Read(), "Нет записи №2");
                    Assert.AreEqual(2, reader.GetInt32(reader.GetOrdinal("Value")));
                    Assert.AreEqual(1L, reader.GetInt64(reader.GetOrdinal("Index")));
                    Assert.AreEqual(objectId, reader.GetGuid(reader.GetOrdinal("ParentId")));
                    Assert.IsFalse(reader.Read(), "Слишком много записей");
                }
            }
        }
    }
}