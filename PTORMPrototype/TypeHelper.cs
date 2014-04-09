using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace PTORMPrototype
{
    public static class TypeHelper
    {
        public static bool IsNullable(this Type type)
        {                     
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>);
        }

        public static bool TryExtractFromNullable(this Type type, out Type innerType)
        {
            if (type.IsNullable())
            {
                innerType = type.GetGenericArguments()[0];
                return true;
            }
            innerType = type;
            return false;
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

        public static Type GetCollectionType(this Type type)
        {
            return type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
        }

        public static bool IsPrimitiveCollection(this Type type)
        {
            return type.IsGenericType && type.IsCollection() &&  type.GetGenericArguments()[0].IsSqlPrimitive();
        }

        static readonly Dictionary<Type,SqlDbType> TypeMapping = new Dictionary<Type, SqlDbType>
        {
            {typeof(string), SqlDbType.NVarChar},
            {typeof(int), SqlDbType.Int},
            {typeof(Guid), SqlDbType.UniqueIdentifier}
        };

        public static SqlDbType GetSqlType(this Type type)
        {
            return TypeMapping[type];
        }

    }
}