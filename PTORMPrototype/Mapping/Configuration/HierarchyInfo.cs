using System.Collections.Generic;

namespace PTORMPrototype.Mapping.Configuration
{
    public class HierarchyInfo
    {
        private readonly List<TypeMappingInfo> _typeMappings = new List<TypeMappingInfo>();

        public IList<TypeMappingInfo> TypeMappings
        {
            get { return _typeMappings; }            
        }

        public TypeMappingInfo BaseType { get; set; }

        public void AddMapping(TypeMappingInfo childType)
        {
            childType.Discriminator = _typeMappings.Count + 1;
            _typeMappings.Add(childType);
        }
    }
}