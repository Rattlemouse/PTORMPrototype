using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace PTORMPrototype.Mapping.Configuration
{
    public class TypeMappingConfig
    {
        private readonly FluentConfiguration _configuration;
        private readonly TypeMappingInfo _mapping;       
        private readonly Type _type;
        private readonly IList<PropertyInfo> _properties = new List<PropertyInfo>();

        public TypeMappingInfo Mapping {  get { return _mapping; }}
        public IList<PropertyInfo> MappedProperties { get { return _properties; } }

        public TypeMappingConfig(FluentConfiguration fluentConfiguration, Type type)
        {
            if (fluentConfiguration == null) 
                throw new ArgumentNullException("fluentConfiguration");
            if (type == null) 
                throw new ArgumentNullException("type");
            _configuration = fluentConfiguration;
            _type = type;
            _mapping = new TypeMappingInfo { Type = type, IdentityField = fluentConfiguration.IdPropertyName };
        }

        public TypeMappingConfig IdentityProperty(Expression<MemberExpression> propertyExpression)
        {
            if (propertyExpression == null) 
                throw new ArgumentNullException("propertyExpression");
            return IdentityProperty(((MemberExpression)propertyExpression.Body).Member.Name);
        }

        public TypeMappingConfig IdentityProperty(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) 
                throw new ArgumentException("name");
            _mapping.IdentityField = name;
            return this;
        }

        public TypeMappingConfig AllProperties()
        {
            foreach (var propertyInfo in _type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(z => z.Name != _mapping.IdentityField))
            {
                _properties.Add(propertyInfo);
            }
            return this;
        }
    }
}