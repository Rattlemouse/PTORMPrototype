using System.Collections.Generic;
using System.Linq;
using PTORMPrototype.Mapping;
using PTORMPrototype.Mapping.Configuration;

namespace PTORTMTests.TestClasses
{
    public class TestProvider : IMetaInfoProvider
    {
        private readonly IDictionary<string,TypeMappingInfo> _dict;

        public TestProvider(IEnumerable<TypeMappingInfo> types)
        {
            _dict = types.ToDictionary(z => z.Type.Name);
        }

        public TypeMappingInfo GetTypeMapping(string type)
        {
            return _dict[type];
        }
    }
}
