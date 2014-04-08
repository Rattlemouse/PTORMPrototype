using System;

namespace PTORMPrototype.Entities
{
    public class BaseClass
    {
        public Guid ObjectId { get;set;}
        public string BaseProperty { get;set;}
    }

    public class Derived0 : BaseClass 
    {
        public int DerivedProp0_0 { get; set;}
        public int DerivedProp0_1 { get; set;}
        public int DerivedProp0_2 { get; set;}
        public int DerivedProp0_3 { get; set;}
        public int DerivedProp0_4 { get; set;}
    }
}