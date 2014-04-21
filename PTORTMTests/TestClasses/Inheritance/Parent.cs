using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructureMap.Pipeline;

namespace PTORTMTests.TestClasses.Inheritance
{
    public class Parent
    {
        public Guid ObjectId { get; set; }
        public string ParentProperty { get; set; }
        public Child Child { get; set; }
    }
}
