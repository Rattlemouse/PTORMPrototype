using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using PTORMPrototype.Mapping;
using PTORMPrototype.Mapping.Configuration;
using Rhino.Mocks;

namespace PTORTMTests
{
    [TestFixture]
    public class PerfTests
    {
        private const string IdentityField = "ObjectId";

        [Test]
        public void TestConfig()
        {
            var stopWatch = new Stopwatch();

            var types = typeof (BaseClass_5).Assembly.GetTypes()
                    .Where(z => z.Name.StartsWith("BaseClass") || z.Name.StartsWith("Derived")).ToArray();
            var list = new List<Delegate>(types.Length * 2);
            stopWatch.Start();
            
            var config =
                FluentConfiguration.Start().DefaultDiscriminatorColumnName("_dscr").DefaultIdProperty(IdentityField);

            foreach (var type in types)
            {
                config.AddTypeAuto(type);
            }
            var elapsed = stopWatch.Elapsed;
            var mappings = config.GenerateTypeMappings() as IList<TypeMappingInfo>;
            Assert.IsNotNull(mappings);
            var mapper = new SqlValueMapper();
            var elapsedBuild = stopWatch.Elapsed;
            foreach (var mapping in mappings)
            {
                list.Add(mapper.GetTypeInstanceCreateDelegate(mapping));
                foreach(var table in mapping.Tables)
                    list.Add(mapper.GetFiller(mapping, table));
            }            

            stopWatch.Stop();
            Debug.WriteLine("Completed full initialization in {0}", stopWatch.Elapsed);
            Debug.WriteLine("Typeload: {0}", elapsed);
            Debug.WriteLine("Build: {0}", elapsedBuild - elapsed);
            Debug.WriteLine("Mapping compile: {0}", stopWatch.Elapsed - elapsedBuild);
            Debug.WriteLine("Types: {0}", mappings.Count);
            Debug.WriteLine("NavProps: {0}", mappings.Sum(z => z.Tables.Sum(t => t.Columns.OfType<NavigationPropertyMapping>().Count())));
            Debug.WriteLine("Props: {0}", mappings.Sum(z => z.Tables.Sum(t => t.Columns.Count(p => p.GetType() == typeof(PropertyMapping)))));
            Assert.Pass();
            
        }
    }
}
