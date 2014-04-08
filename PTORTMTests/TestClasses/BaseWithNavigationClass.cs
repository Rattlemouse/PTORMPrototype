using System;

namespace PTORTMTests.TestClasses
{
    public class BaseWithNavigationClass
    {
        public Guid ObjectId { get; set; }
        public NavigationedClass Nav { get; set; }
      //  public IList<NavigationedClass> Navs { get; set; }        
    }
}