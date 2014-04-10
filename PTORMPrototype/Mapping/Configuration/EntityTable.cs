using System.Collections.Generic;

namespace PTORMPrototype.Mapping.Configuration
{
    public class EntityTable : Table
    {
        public string DiscriminatorColumn { get; set; }
        public PropertyMapping IdentityColumn { get; set; }
        
        private readonly IList<PropertyMapping> _columns = new List<PropertyMapping>();

        public bool HasDiscriminator
        {
            get { return DiscriminatorColumn != null; }
        }

        public override IList<PropertyMapping> Columns
        {
            get { return _columns; }
        }
    }
}