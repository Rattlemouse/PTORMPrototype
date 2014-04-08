using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTORTMTests.MassTest
{
    public class FirstClass
    {
        public Guid ObjectId { get; set; }        
        public int FP1 { get; set; }
        public int FP2 { get; set; }
        public int FP3 { get; set; }
        public int FP4 { get; set; }
        public string FP5 { get; set; }
        public string FP6 { get; set; }
        public string FP7 { get; set; }
        public string FP8 { get; set; }
        public SecondClass Second { get; set; }
    }

    public class SecondClass
    {
        public Guid ObjectId { get; set; }     
        public int FP1 { get; set; }
        public int FP2 { get; set; }
        public int FP3 { get; set; }
        public int FP4 { get; set; }
        public string FP5 { get; set; }
        public string FP6 { get; set; }
        public string FP7 { get; set; }
        public string FP8 { get; set; }

        public ThirdClass Third { get; set; }
    }

    public class ThirdClass
    {
        public Guid ObjectId { get; set; }
        public int FP1 { get; set; }
        public int FP2 { get; set; }
        public int FP3 { get; set; }
        public int FP4 { get; set; }
        public string FP5 { get; set; }
        public string FP6 { get; set; }
        public string FP7 { get; set; }
        public string FP8 { get; set; }
    }
}
