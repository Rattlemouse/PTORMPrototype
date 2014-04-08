using System.Collections.Generic;

namespace PTORMPrototype.Mapping.Configuration
{
    public class TableInfo
    {
        private readonly IList<PropertyMapping> _columns = new List<PropertyMapping>();
        public string Name { get; set; }
        public string DiscriminatorColumn { get; set; }
        public string IdentityColumn { get; set; }

        public IList<PropertyMapping> Columns
        {
            get { return _columns; }
        }
    }
}