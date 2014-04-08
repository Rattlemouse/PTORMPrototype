using PTORMPrototype.Mapping.Configuration;

namespace PTORMPrototype.Mapping
{
    public interface IMetaInfoProvider
    {
        TypeMappingInfo GetTypeMapping(string type);        
    }
}