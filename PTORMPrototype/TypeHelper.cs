using System;
using System.Collections;
using System.Linq;

namespace PTORMPrototype
{
    public static class TypeHelper
    {
        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>);
        }
        private static readonly Type[] Primitives = { typeof(string), typeof(Guid), typeof(byte[]) };
        public static bool IsSqlPrimitive(this Type type)
        {
            return type.IsPrimitive || Primitives.Contains(type);
        }

        public static bool IsCollection(this Type type)
        {
            return type.GetInterfaces().Contains(typeof (IEnumerable));
        }

        public static bool IsPrimitiveCollection(this Type type)
        {
            return type.IsGenericType && type.IsCollection() &&  type.GetGenericArguments()[0].IsSqlPrimitive();
        }
    }
}