using System.Collections.Generic;

namespace PTORMPrototype.Query
{
    public class UpdatePlan
    {
        public ICollection<UpdatePart> Parts { get; set; }
    }
}