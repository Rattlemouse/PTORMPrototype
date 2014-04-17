using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTORTMTests.TestClasses.Collections
{
    public class ClassWithCollection
    {
        public Guid ObjectId { get; set; }
        public ICollection<CollectionItem> Collection { get; set; }

    }
}
