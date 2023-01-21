using System.Reflection.Emit;

namespace System.Reflection
{
    internal sealed class MockedAssembly
    {
        private sealed record MockedType(Type Type);
        public static MockedAssembly Instance { get; } = new();
        public ModuleBuilder Builder { get; }
        private MockedAssembly()
        {
            var assemblyName = new AssemblyName($"Mock{Guid.NewGuid()}");
            assemblyName.SetPublicKey(Assembly.GetExecutingAssembly().GetName().GetPublicKey());
            var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run); ;
            Builder = assembly.DefineDynamicModule(assemblyName.Name!);
        }
        private static readonly Dictionary<Type, MockedType> Types = new();
        public Type? GetMockedType(Type baseType)
        {
            if (!Types.ContainsKey(baseType))
                Types.Add(baseType, new(DefineNewImplementation(baseType)));
            return Types[baseType].Type;
        }
        public Type? GetMockedType<T>()
            => GetMockedType(typeof(T));
        public object CreateInstance(Type type, params object[]? args)
            => Activator.CreateInstance(GetMockedType(type)!, args)!;
        public T CreateInstance<T>(params object[]? args)
            => (T)Activator.CreateInstance(GetMockedType<T>()!, args)!;
        private static string ToSignature(MethodInfo methodInfo)
            => $"{methodInfo.Name}_{methodInfo.ReturnType.Name}_{string.Join(',', methodInfo.GetParameters().Select(x => x.Name))}";
        private static string GetPrivateFieldForPropertyName(string propertyName)
            => $"<{propertyName}>k__BackingField";
        private Type DefineNewImplementation(Type type)
        {
            string name = $"{type.Name}{string.Join('_', type.GetGenericArguments().Select(x => x.Name))}Concretization";
            var createdNames = new Dictionary<string, bool>();
            var typeBuilder = Builder.DefineType(name, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);
            if (!type.IsInterface)
                typeBuilder.SetParent(type);
            if (type.IsInterface)
                typeBuilder.AddInterfaceImplementation(type);

            List<ILGenerator> constructorGenerators = new();
            var constructors = type.GetConstructors();
            if (constructors.Length == 0)
            {
                var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
                var constructorGenerator = constructorBuilder.GetILGenerator();
                constructorGenerators.Add(constructorGenerator);
            }
            else
            {
                foreach (var constructor in constructors)
                {
                    var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, constructor?.GetParameters().Select(x => x.ParameterType).ToArray() ?? Type.EmptyTypes);
                    var constructorGenerator = constructorBuilder.GetILGenerator();
                    constructorGenerators.Add(constructorGenerator);
                }
            }

            ConfigureProperties(type, typeBuilder, constructorGenerators, createdNames);
            ConfigureMethods(type, typeBuilder, createdNames);
            if (constructors.Length == 0)
                constructorGenerators.First().Emit(OpCodes.Ret);
            else
            {
                var constructorIterator = constructors.GetEnumerator();
                foreach (var constructorGenerator in constructorGenerators)
                {
                    constructorIterator.MoveNext();
                    var constructorInfo = (constructorIterator.Current as ConstructorInfo)!;
                    constructorGenerator.Emit(OpCodes.Ldarg_0);
                    int counter = 1;
                    foreach (var parameter in constructorInfo.GetParameters())
                        constructorGenerator.Emit(OpCodes.Ldarg, counter++);
                    constructorGenerator.Emit(OpCodes.Call, constructorInfo);
                    constructorGenerator.Emit(OpCodes.Ret);
                }
            }
            var createdType = typeBuilder!.CreateType();
            return createdType!;
        }
        private void ConfigureProperties(Type currentType, TypeBuilder typeBuilder, List<ILGenerator> constructorGenerators, Dictionary<string, bool> createdNames)
        {
            foreach (var property in currentType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => !createdNames.ContainsKey(x.Name) && (currentType.IsInterface || x.GetMethod!.IsAbstract)))
            {
                ConfigureProperty(property, typeBuilder, constructorGenerators, createdNames);
            }
            foreach (var subType in currentType.GetInterfaces())
            {
                ConfigureProperties(subType, typeBuilder, constructorGenerators, createdNames);
            }
            if (currentType.BaseType != null && currentType.BaseType != typeof(object))
                ConfigureProperties(currentType.BaseType, typeBuilder, constructorGenerators, createdNames);
        }

#pragma warning disable IDE0079 // Remove unnecessary suppression
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "We need this parameter to simulate a dictionary method.")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
        private static object GetFromDictionary(object dictionary, object key)
        {
            return new();
        }
        private static MethodBuilder CreateMethod(MethodInfo methodInfo, TypeBuilder typeBuilder, Dictionary<string, bool> createdNames, Action<ILGenerator>? action = null, bool returnDefault = false)
        {
            //todo: it doesn't work with generic methods, protected and private protected too
            string signature = ToSignature(methodInfo);
            if (!createdNames.ContainsKey(signature))
            {
                createdNames.Add(signature, true);
                var parameters = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
                MethodBuilder methodBuilder = typeBuilder.DefineMethod(methodInfo.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                    methodInfo.ReturnType, parameters);
                var methodGenerator = methodBuilder.GetILGenerator();
                if (action != null)
                {
                    action.Invoke(methodGenerator);
                }
                else if (returnDefault)
                {
                    if (methodInfo.ReturnType != null && methodInfo.ReturnType != typeof(void))
                    {
                        if (Check(methodInfo.ReturnType))
                            methodGenerator.Emit(OpCodes.Ldc_I4_0);
                        else
                            methodGenerator.Emit(OpCodes.Ldnull);
                    }
                }
                else
                {
                    methodGenerator.Emit(OpCodes.Ldstr, methodInfo.Name);
                    methodGenerator.Emit(OpCodes.Stloc_0);
                    methodGenerator.Emit(OpCodes.Ldarg_0);
                    methodGenerator.Emit(OpCodes.Ldloc_0);
                    methodGenerator.Emit(OpCodes.Ldc_I4, parameters.Length);
                    methodGenerator.Emit(OpCodes.Newarr, typeof(object));
                    methodGenerator.Emit(OpCodes.Stloc_1);
                    methodGenerator.Emit(OpCodes.Ldloc_1);
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        methodGenerator.Emit(OpCodes.Ldc_I4, i);
                        methodGenerator.Emit(OpCodes.Ldarg, i + 1);
                        if (parameters[i] != typeof(string) && Check(parameters[i]))
                            methodGenerator.Emit(OpCodes.Box, parameters[i]);
                        methodGenerator.Emit(OpCodes.Stelem_Ref);
                    }
                    methodGenerator.Emit(OpCodes.Ldloc_1);
                    methodGenerator.Emit(OpCodes.Call, typeof(DecoratedMock).GetMethod("InvokeMethod")!);
                }
                methodGenerator.Emit(OpCodes.Ret);
                return methodBuilder;
            }
            return default!;
        }
        private static bool Check(Type type)
        {
            foreach (Type typeR in NormalTypes)
                if (typeR == type)
                    return true;
            return false;
        }
        private static readonly List<Type> NormalTypes = new()
        {
            typeof(int),
            typeof(bool),
            typeof(char),
            typeof(decimal),
            typeof(double),
            typeof(long),
            typeof(byte),
            typeof(sbyte),
            typeof(float),
            typeof(uint),
            typeof(ulong),
            typeof(short),
            typeof(ushort),
            typeof(string),
            typeof(int?),
            typeof(bool?),
            typeof(char?),
            typeof(decimal?),
            typeof(double?),
            typeof(long?),
            typeof(byte?),
            typeof(sbyte?),
            typeof(float?),
            typeof(uint?),
            typeof(ulong?),
            typeof(short?),
            typeof(ushort?),
            typeof(Guid),
            typeof(Guid?)
        };
        private void ConfigureProperty(PropertyInfo property, TypeBuilder typeBuilder, List<ILGenerator> constructorGenerators, Dictionary<string, bool> createdNames)
        {
            string privateFieldName = GetPrivateFieldForPropertyName(property.Name);

            if (!createdNames.ContainsKey(privateFieldName))
            {
                if (property.GetMethod == null)
                    return;
                var getParameters = property.GetMethod!.GetParameters().Select(x => x.ParameterType).ToArray();

                var isIndexer = getParameters.Length > 0;
                Type propertyType = !isIndexer ? property.PropertyType : typeof(Dictionary<,>).MakeGenericType(typeof(string), property.PropertyType);

                var privateFieldBuilder = typeBuilder.DefineField(privateFieldName, propertyType, FieldAttributes.Private);
                createdNames.Add(privateFieldName, true);

                if (isIndexer)
                {
                    foreach (var constructorGenerator in constructorGenerators)
                    {
                        constructorGenerator.Emit(OpCodes.Ldarg_0);
                        constructorGenerator.Emit(OpCodes.Newobj, propertyType.GetConstructor(Type.EmptyTypes)!);
                        constructorGenerator.Emit(OpCodes.Stfld, privateFieldBuilder);
                    }
                }

                var propertyBuilder = typeBuilder.DefineProperty(property.Name, PropertyAttributes.None, property.PropertyType, null);
                MethodBuilder getMethodBuilder = CreateMethod(property.GetMethod, typeBuilder, createdNames, (generator) =>
                {
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldfld, privateFieldBuilder);
                    if (isIndexer)
                    {
                        for (int i = 0; i < getParameters.Length; i++)
                            generator.Emit(OpCodes.Ldarg_S, i + 1);
                        generator.Emit(OpCodes.Callvirt, typeof(MockedAssembly).GetMethod(nameof(GetFromDictionary), BindingFlags.NonPublic | BindingFlags.Static)!);
                    }
                });
                propertyBuilder.SetGetMethod(getMethodBuilder);

                if (property.SetMethod != null)
                {
                    var setParameters = property.SetMethod.GetParameters().Select(x => x.ParameterType).ToArray();
                    MethodBuilder setMethodBuilder = CreateMethod(property.SetMethod, typeBuilder, createdNames,
                        (generator) =>
                        {
                            generator.Emit(OpCodes.Ldarg_0);
                            generator.Emit(OpCodes.Ldarg_1);
                            if (isIndexer)
                            {
                                for (int i = 0; i < setParameters.Length; i++)
                                    generator.Emit(OpCodes.Ldarg_S, i + 2);
                                generator.Emit(OpCodes.Callvirt, privateFieldBuilder.FieldType.GetMethod("Add")!);
                            }
                            else
                                generator.Emit(OpCodes.Stfld, privateFieldBuilder);
                        });
                    propertyBuilder.SetSetMethod(setMethodBuilder);
                }
            }
        }
        private void ConfigureMethods(Type currentType, TypeBuilder typeBuilder, Dictionary<string, bool> createdNames)
        {
            foreach (var method in currentType.GetMethods()
                .Where(x => x.IsAbstract))
            {
                _ = CreateMethod(method, typeBuilder, createdNames, null, true);
            }
            foreach (var subType in currentType.GetInterfaces())
            {
                ConfigureMethods(subType, typeBuilder, createdNames);
            }
            if (currentType.BaseType != null && currentType.BaseType != typeof(object))
                ConfigureMethods(currentType.BaseType, typeBuilder, createdNames);
        }
        private class DecoratedMock
        {
            public static object? InvokeMethod(object entity, string methodName, params object[] parameters)
            {
                Type type = entity.GetType();
                var method = type.GetMethod(methodName);
                if (method == null)
                    return default;
                var result = method.Invoke(entity, parameters);
                if (result == null)
                    return default;
                return result;
            }
        }
    }
}
