using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using PTORMPrototype.Mapping.Configuration;
using PTORMPrototype.Query;

namespace PTORMPrototype.Mapping
{
    public class SqlValueMapper
    {        
        private static readonly MethodInfo GetInt = typeof(SqlDataReader).GetMethod("GetInt32", new[] { typeof(int) });
        private static readonly MethodInfo GetGuid = typeof(SqlDataReader).GetMethod("GetGuid", new[] { typeof(int) });
        private static readonly MethodInfo GetString = typeof(SqlDataReader).GetMethod("GetString", new[] { typeof(int) });
        private static readonly MethodInfo IsDbNullMethod = typeof(IDataRecord).GetMethod("IsDBNull", new[] { typeof(int) });

        private class LayerInfo
        {
            public TypePart TypePart { get; set; }
            public CreatingDelegate CreatingDelegate { get; set; }            
            public List<FillingDelegate> FillingDelegates { get; set; }
            public int LayerOffset { get; set; }
            public LayerInfo NextLayer { get; set; }

            public void AddNextLayer(LayerInfo layer)
            {
                NextLayer = layer;
                layer.LayerOffset = LayerOffset + 1 + TypePart.Tables.Sum(z => z.Columns.Count + 1);
            }
        }

        public IEnumerable MapFromReader(SqlDataReader reader, SelectClause selectClause)
        {                                    
            var successRead = reader.Read();
            LayerInfo firstLayer = null;
            LayerInfo lastLayer = null;
            foreach (var typePart in selectClause.Parts.OfType<TypePart>())
            {
                var layer = new LayerInfo
                {
                    TypePart = typePart,
                    CreatingDelegate = GetTypeInstanceCreateDelegate(typePart.Type),
                    FillingDelegates = typePart.Tables.Select(z => GetFiller(typePart.Type, z)).ToList()                    
                };
                if (lastLayer != null)
                    lastLayer.AddNextLayer(layer);
                if (firstLayer == null)
                    firstLayer = layer;
                lastLayer = layer;
            }
            while (successRead)
            {
                yield return LoadObjectTree(firstLayer, reader, out successRead);
            }            
        }

        private object LoadObjectTree(LayerInfo layer, IDataReader reader, out bool successRead)
        {
            var val = (IComparable)reader.GetValue(layer.LayerOffset);
            var createObject = layer.CreatingDelegate(reader, layer.LayerOffset);
            var notfirst = false;
            foreach (var fillingDelegate in layer.FillingDelegates)
            {
                fillingDelegate(reader, createObject, layer.LayerOffset + (notfirst ? 1 : 2));
                notfirst = true;
            }
            successRead = true;
            if (layer.NextLayer != null)
            {
                //todo: nav-filling delegates - separate
                var nextPart = (SubTypePart) layer.NextLayer.TypePart;
                var property = createObject.GetType().GetProperty(nextPart.CollectingProperty.Name);
                while (successRead && ((IComparable) reader.GetValue(layer.LayerOffset)).CompareTo(val) == 0)
                {
                    var propObj = LoadObjectTree(layer.NextLayer, reader, out successRead);
                    property.SetValue(createObject, propObj);
                }
            }
            else
            {
                successRead = reader.Read();
            }
            return createObject;

        }


        //todo: temporary public
        public CreatingDelegate GetTypeInstanceCreateDelegate(TypeMappingInfo mapping)
        {

            var ct = typeof(object);
            Type[] methodArgs2 = { typeof(IDataRecord), typeof(int) };

            var method = new DynamicMethod(
                "ct",
                ct,
                methodArgs2, typeof(SqlValueMapper));
            var generator = method.GetILGenerator();
            var localV = generator.DeclareLocal(ct);
            var hierarchy = mapping.Hierarchy;
            var type = mapping.Type;
            if (hierarchy == null || hierarchy.TypeMappings.Count == 1)
            {
                generator.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
            }
            else
            {                
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Ldc_I4, 1);
                generator.Emit(OpCodes.Add);
                generator.Emit(OpCodes.Call, GetInt);
                
                var endOfMethod = generator.DefineLabel();
                var defaultLabel = generator.DefineLabel();
                var jumpTable = hierarchy.TypeMappings.Select(z => generator.DefineLabel()).ToArray();

                generator.Emit(OpCodes.Switch, jumpTable);

                for (var i = 0; i < jumpTable.Length; i++)
                {
                    generator.MarkLabel(jumpTable[i]);
                    generator.Emit(OpCodes.Newobj, hierarchy.TypeMappings[i].Type.GetConstructor(Type.EmptyTypes));
                    generator.Emit(OpCodes.Br, endOfMethod);
                }

                generator.MarkLabel(defaultLabel);
                //todo: make detailed
                generator.ThrowException(typeof(InvalidOperationException));
                generator.MarkLabel(endOfMethod);
            }
            generator.Emit(OpCodes.Stloc, localV);
            generator.Emit(OpCodes.Ldloc, localV);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldc_I4, 0);
            generator.Emit(OpCodes.Add);
            var idProperty = type.GetProperty(mapping.IdentityField);
            generator.Emit(OpCodes.Call, GetGetterMethod(idProperty.PropertyType, false));
            generator.Emit(OpCodes.Call, idProperty.GetSetMethod());
            generator.Emit(OpCodes.Ldloc, localV);
            generator.Emit(OpCodes.Ret);
            return (CreatingDelegate)method.CreateDelegate(typeof(CreatingDelegate));

        }

        //todo: temporary public
        public FillingDelegate GetFiller(TypeMappingInfo mapping, TableInfo table) 
        {            
            var ct = typeof(object);
            // Fill(reader, obj, offset)
            Type[] methodArgs2 = { typeof(IDataRecord), ct, typeof(int) };
            var method = new DynamicMethod(
                "ct",
                null,
                methodArgs2, typeof(SqlValueMapper));

            var generator = method.GetILGenerator();
            
            Type type = mapping.Type;
            
            var i = 0;
            foreach(var prop in table.Columns)
            {
                var navigation = prop as NavigationPropertyMapping;
                if (navigation != null)
                {
                    GenerateForNavigationProperty(navigation, type, generator, i);
                }
                else
                {
                    GenerateForPrimitive(type, prop, generator, i);
                }
                i++;
            }
            // return
            generator.Emit(OpCodes.Ret);
            return (FillingDelegate)method.CreateDelegate(typeof(FillingDelegate));
        }

        private void GenerateForPrimitive(Type type, PropertyMapping prop, ILGenerator generator, int i)
        {
            var propertyInfo = type.GetProperty(prop.Name);
            if (propertyInfo == null)
                throw new InvalidOperationException("Class does not match mapping. This is really strange.");
            var propertyType = propertyInfo.PropertyType;
            var canBeNull = prop.Nullable || propertyType == typeof (string);
            var setMethod = propertyInfo.GetSetMethod();
            if (setMethod == null)
                throw new InvalidOperationException("Mapped property must have set method.");
            var endIfLabel = generator.DefineLabel();
            if (canBeNull)
            {
                // if(!reader.IsDbNull(i + offset))
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldarg_2);
                generator.Emit(OpCodes.Ldc_I4, i);
                generator.Emit(OpCodes.Add);
                generator.Emit(OpCodes.Callvirt, IsDbNullMethod);
                generator.Emit(OpCodes.Brtrue, endIfLabel);
            }
            // obj.Property = reader.GetInt(i + offset);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Ldc_I4, i);
            generator.Emit(OpCodes.Add);
            generator.Emit(OpCodes.Call, GetGetterMethod(propertyType, prop.Nullable));
            if (prop.Nullable)
            {
                // obj.Property = new Nullable<int>(reader.GetInt(i + offset));
                generator.Emit(OpCodes.Newobj,
                    propertyType.GetConstructor(propertyType.GetGenericArguments()));
            }
            generator.Emit(OpCodes.Call, setMethod);
            // end if
            if (canBeNull)
                generator.MarkLabel(endIfLabel);
        }

        private void GenerateForNavigationProperty(NavigationPropertyMapping navigation, Type type, ILGenerator generator,
            int i)
        {
            var targetMapping = navigation.TargetType;
            var targetClrType = targetMapping.Type;
            var propertyInfo = type.GetProperty(navigation.Name);
            if (propertyInfo == null)
                throw new InvalidOperationException("Class does not match mapping. This is really strange.");
            var setMethod = propertyInfo.GetSetMethod();
            if (setMethod == null)
                throw new InvalidOperationException("Mapped property must have set method.");
            var endIfLabel = generator.DefineLabel();
            // if(!reader.IsDbNull(i + offset))
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Ldc_I4, i);
            generator.Emit(OpCodes.Add);
            generator.Emit(OpCodes.Callvirt, IsDbNullMethod);
            generator.Emit(OpCodes.Brtrue, endIfLabel);
            var navVar = generator.DeclareLocal(targetClrType);
            var navIdentityProp = targetClrType.GetProperty(targetMapping.IdentityField);
            // obj.NavProperty = new ReferencedType() { ObjectId = reader.GetGuid(i+offset) };
            generator.Emit(OpCodes.Newobj, targetClrType.GetConstructor(Type.EmptyTypes));
            generator.Emit(OpCodes.Stloc, navVar);
            generator.Emit(OpCodes.Ldloc, navVar);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Ldc_I4, i);
            generator.Emit(OpCodes.Add);
            generator.Emit(OpCodes.Call, GetGetterMethod(navIdentityProp.PropertyType, false));
            generator.Emit(OpCodes.Call, navIdentityProp.GetSetMethod());

            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldloc, navVar);
            generator.Emit(OpCodes.Call, setMethod);
            // end if
            generator.MarkLabel(endIfLabel);
        }

        private MethodInfo GetGetterMethod(Type propertyType, bool nullable)
        {
            if (nullable)
                propertyType = propertyType.GetGenericArguments()[0];
            if (propertyType == typeof (int))
                return GetInt;
            if (propertyType == typeof (Guid))
                return GetGuid;
            if (propertyType == typeof(string))
                return GetString;
            throw new InvalidOperationException(string.Format("Unknown method for type {0}", propertyType));
        }

        
    }
}