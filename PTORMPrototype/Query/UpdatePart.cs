using System.Collections;
using System.Collections.Generic;
using PTORMPrototype.Mapping.Configuration;

namespace PTORMPrototype.Query
{
    public class UpdatePart
    {
        public UpdatePart()
        {
            Parameters = new List<Parameter>();
        }

        public Table Table { get; set; }
        public string SqlString { get; set; }
        public IList<Parameter> Parameters { get; private set; }
    }
}