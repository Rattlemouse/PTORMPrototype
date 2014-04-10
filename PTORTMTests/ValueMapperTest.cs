using System;
using System.Data;
using System.Linq;
using NUnit.Framework;
using PTORMPrototype.Mapping;
using PTORMPrototype.Mapping.Configuration;
using PTORMPrototype.Query;
using PTORTMTests.TestClasses;
using PTORTMTests.TestClasses.Collections;
using Rhino.Mocks;

namespace PTORTMTests
{
    [TestFixture]
    public class ValueMapperTest
    {
        private const string IdentityField = "ObjectId";
        private const string Discriminator = "_dscr";      
        [Test]
        public void TestDerivedClassMapping()
        {                 
        
            var types = FluentConfiguration.Start().DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName(Discriminator)
                .AddType<BaseClass>(z => z.AllProperties())
                .AddType<DerivedClass>(z => z.AllProperties())
                .GenerateTypeMappings();
            var provider = new TestProvider(types);    
            
            var mapper = new SqlValueMapper();
            var reader = MockRepository.GenerateMock<IDataReader>();
            reader.Stub(z => z.GetGuid(0)).Return(Guid.NewGuid());
            reader.Stub(z => z.GetInt32(1)).Return(2);
            reader.Stub(z => z.GetInt32(2)).Return(1);
            reader.Stub(z => z.GetGuid(3)).Return(Guid.NewGuid());
            reader.Stub(z => z.GetInt32(4)).Return(3);
            var numCalls = 1;
            reader.Stub(z => z.Read()).Return(true).WhenCalled(z => z.ReturnValue = --numCalls >= 0 );
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

        [Test]
        public void TestSimpleClassMapping()
        {
            var types = FluentConfiguration.Start().DefaultIdProperty(IdentityField).AddType<BaseClass>(z => z.AllProperties()).GenerateTypeMappings();
            var provider = new TestProvider(types);    
            
            var mapper = new SqlValueMapper();
            var reader = MockRepository.GenerateMock<IDataReader>();
            reader.Stub(z => z.GetGuid(0)).Return(Guid.NewGuid());
            reader.Stub(z => z.GetInt32(1)).Return(2);
            reader.Stub(z => z.GetInt32(2)).Return(1);            
            var numCalls = 1;
            reader.Stub(z => z.Read()).Return(true).WhenCalled(z => z.ReturnValue = --numCalls >= 0 );
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

        [Test]
        public void TestMappingWithNavigation()
        {
            var navGuid = Guid.NewGuid();
            var types = FluentConfiguration.Start()
                .DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName(Discriminator)
                .AddType<BaseWithNavigationClass>(z => z.AllProperties())
                .AddType<NavigationedClass>(z => z.AllProperties())
                .GenerateTypeMappings();
            var provider = new TestProvider(types);
            var mapper = new SqlValueMapper();
            var reader = MockRepository.GenerateMock<IDataReader>();
            reader.Stub(z => z.GetGuid(0)).Return(Guid.NewGuid());
            reader.Stub(z => z.GetInt32(1)).Return(2);
            reader.Stub(z => z.GetGuid(2)).Return(navGuid);            
            var numCalls = 1;
            reader.Stub(z => z.Read()).Return(true).WhenCalled(z => z.ReturnValue = --numCalls >= 0 );
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

        [Test]
        public void TestMappingWithNavigationCollection()
        {
            var navGuid = Guid.NewGuid();
            var types = FluentConfiguration.Start()
                .DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName(Discriminator)
                .AddType<ClassWithCollection>(z => z.AllProperties())
                .AddType<CollectionItem>(z => z.AllProperties())
                .GenerateTypeMappings();
            var provider = new TestProvider(types);
            var mapper = new SqlValueMapper();
            var reader = MockRepository.GenerateMock<IDataReader>();
            var id = Guid.NewGuid();
            reader.Stub(z => z.GetGuid(0)).Return(id);
            reader.Stub(z => z.GetValue(0)).Return(id);
            reader.Stub(z => z.GetInt32(1)).Return(1);
            reader.Stub(z => z.GetGuid(2)).Return(navGuid);
            reader.Stub(z => z.GetValue(2)).Return(navGuid);
            reader.Stub(z => z.GetInt32(3)).Return(1);
            reader.Stub(z => z.GetGuid(4)).Return(id);
            reader.Stub(z => z.GetString(5)).Return("someString");
            var numCalls = 1;
            reader.Stub(z => z.Read()).Return(true).WhenCalled(z => z.ReturnValue = --numCalls >= 0);
            var typeMappingInfo = provider.GetTypeMapping("ClassWithCollection");
            var itemMappingInfo = provider.GetTypeMapping("CollectionItem");
            var selectClause = new SelectClause
            {
                Parts = new SelectPart[]
                {
                    new TypePart
                    {
                        Type = typeMappingInfo, 
                        Tables = typeMappingInfo.Tables.Take(1).ToList()
                    },
                    new SubTypePart
                    {
                        Type = itemMappingInfo,
                        Tables = itemMappingInfo.Tables.Take(1).ToList(),
                        CollectingProperty = typeMappingInfo.GetProperty("Collection"),
                        CollectingType = typeMappingInfo
                    }
                }
            };
            var classes = mapper.MapFromReader(reader, selectClause);
            var result = classes.OfType<ClassWithCollection>().First();
            Assert.IsNotNull(result.Collection);
            Assert.AreEqual(navGuid, result.Collection.First().ObjectId);
        }

        [Test]
        public void TestMappingWithArrayOfPrimitives()
        {
            var types = FluentConfiguration.Start()
                .DefaultIdProperty(IdentityField)
                .DefaultDiscriminatorColumnName(Discriminator)
                .AddType<ClassWithIntArr>(z => z.AllProperties())                
                .GenerateTypeMappings();
            var provider = new TestProvider(types);
            var mapper = new SqlValueMapper();
            var reader = MockRepository.GenerateMock<IDataReader>();
            var id = Guid.NewGuid();
            var numCalls = 2;
            reader.Stub(z => z.GetGuid(0)).Return(id);
            reader.Stub(z => z.GetValue(0)).Return(id);
            reader.Stub(z => z.GetInt32(1)).Return(1);
            reader.Stub(z => z.GetGuid(2)).Return(id);
            reader.Stub(z => z.GetValue(2)).Return(id);
            reader.Stub(z => z.GetValue(3)).Return(2);
            reader.Stub(z => z.GetInt64(4)).Return(0L).WhenCalled(z => z.ReturnValue = numCalls == 1 ? 0L : 1L);            
            
            reader.Stub(z => z.Read()).Return(true).WhenCalled(z => z.ReturnValue = --numCalls >= 0);
            var typeMappingInfo = provider.GetTypeMapping("ClassWithIntArr");            
            var selectClause = new SelectClause
            {
                Parts = new SelectPart[]
                {
                    new TypePart
                    {
                        Type = typeMappingInfo, 
                        Tables = typeMappingInfo.Tables.Take(1).ToList()
                    },
                    new ExpansionPart
                    {                        
                        Table = typeMappingInfo.Tables.OfType<PrimitiveListTable>().First(),
                        CollectingProperty = typeMappingInfo.GetProperty("Arr"),
                        CollectingType = typeMappingInfo
                    }
                }
            };
            var classes = mapper.MapFromReader(reader, selectClause);
            var result = classes.OfType<ClassWithIntArr>().First();
            Assert.IsNotNull(result.Arr);
            Assert.AreEqual(2, result.Arr.Length);
            Assert.AreEqual(2, result.Arr.First());
        }
    }

}

