using System.Collections.Concurrent;
using System.Reflection.Emit;
using static System.Collections.Specialized.BitVector32;

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
            var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            Builder = assembly.DefineDynamicModule(assemblyName.Name!);
        }
        private static readonly ConcurrentDictionary<Type, MockedType> s_types = new();
        public Type? GetMockedType(Type baseType, Action<MockingConfiguration>? configuration)
        {
            var mockConfiguration = new MockingConfiguration();
            configuration?.Invoke(mockConfiguration);
            if (!s_types.ContainsKey(baseType))
                s_types.TryAdd(baseType, new(DefineNewImplementation(baseType, mockConfiguration)));
            else if (mockConfiguration.CreateNewOneIfExists)
                s_types[baseType] = new(DefineNewImplementation(baseType, mockConfiguration));
            return s_types[baseType].Type;
        }
        public Type? GetMockedType<T>(Action<MockingConfiguration>? configuration)
            => GetMockedType(typeof(T), configuration);
        public object CreateInstance(Type type, Action<MockingConfiguration>? configuration, params object[]? args)
            => Activator.CreateInstance(GetMockedType(type, configuration)!, args)!;
        public T CreateInstance<T>(Action<MockingConfiguration>? configuration, params object[]? args)
            => (T)Activator.CreateInstance(GetMockedType<T>(configuration)!, args)!;
        private static string ToSignature(MethodInfo methodInfo)
            => $"{methodInfo.Name}_{methodInfo.ReturnType.Name}_{string.Join(',', methodInfo.GetParameters().Select(x => x.Name))}";
        private static string GetPrivateFieldForPropertyName(string propertyName)
            => $"<{propertyName}>k__BackingField";
        private Type DefineNewImplementation(Type type, MockingConfiguration configuration)
        {
            var name = configuration.CreateNewOneIfExists ?
                $"{type.Name}{string.Join('_', type.GetGenericArguments().Select(x => x.Name))}{Guid.NewGuid():N}Concretization" :
                $"{type.Name}{string.Join('_', type.GetGenericArguments().Select(x => x.Name))}Concretization";
            var createdNames = new Dictionary<string, bool>();
            var typeBuilder =
                configuration.IsSealed ?
                Builder.DefineType(name, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed) :
                Builder.DefineType(name, TypeAttributes.Public | TypeAttributes.Class);
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
                    var counter = 1;
                    foreach (var parameter in constructorInfo.GetParameters())
                        constructorGenerator.Emit(OpCodes.Ldarg, counter++);
                    constructorGenerator.Emit(OpCodes.Call, constructorInfo);
                    constructorGenerator.Emit(OpCodes.Ret);
                }
            }
            var createdType = typeBuilder!.CreateType();
            return createdType!;
        }
        public Type? CreateFromScratch(string name, Type? parentType, List<MockedProperty> properties)
        {
            var typeBuilder = parentType == null ? Builder.DefineType(name, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed)
                : Builder.DefineType(name, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed, parentType);
            List<ILGenerator> constructorGenerators = new();
            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
            var constructorGenerator = constructorBuilder.GetILGenerator();
            constructorGenerators.Add(constructorGenerator);
            constructorGenerators.First().Emit(OpCodes.Ret);

            foreach (var property in properties)
            {
                var privateFieldName = GetPrivateFieldForPropertyName(property.Name);
                var privateFieldBuilder = typeBuilder.DefineField(privateFieldName, property.Type, FieldAttributes.Private);
                var propertyBuilder = typeBuilder.DefineProperty(property.Name, PropertyAttributes.None, property.Type, null);
                var getMethodName = $"get_{property.Name}";
                var getMethodBuilder = typeBuilder.DefineMethod(getMethodName,
                        MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                        property.Type, Array.Empty<Type>());
                var getMethodGenerator = getMethodBuilder.GetILGenerator();
                getMethodGenerator.Emit(OpCodes.Ldarg_0);
                getMethodGenerator.Emit(OpCodes.Ldfld, privateFieldBuilder);
                getMethodGenerator.Emit(OpCodes.Ret);
                propertyBuilder.SetGetMethod(getMethodBuilder);

                var setMethodName = $"set_{property.Name}";
                var setMethodBuilder = typeBuilder.DefineMethod(setMethodName,
                        MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                        null, new Type[1] { property.Type });
                var setMethodGenerator = setMethodBuilder.GetILGenerator();
                setMethodGenerator.Emit(OpCodes.Ldarg_0);
                setMethodGenerator.Emit(OpCodes.Ldarg_1);
                setMethodGenerator.Emit(OpCodes.Stfld, privateFieldBuilder);
                setMethodGenerator.Emit(OpCodes.Ret);
                propertyBuilder.SetSetMethod(setMethodBuilder);
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
        private static MethodBuilder CreateMethod(MethodInfo methodInfo, TypeBuilder typeBuilder, Dictionary<string, bool> createdNames, Action<ILGenerator>? action = null, bool returnDefault = false,
             Type[][]? parameterTypeRequiredCustomModifiers = null, Type[][]? parameterTypeOptionalCustomModifiers = null)
        {
            //todo: it doesn't work with generic methods, protected and private protected too
            var signature = ToSignature(methodInfo);
            if (!createdNames.ContainsKey(signature))
            {
                createdNames.Add(signature, true);
                var parameters = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
                var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name,
                    methodInfo.Attributes & ~MethodAttributes.Abstract,
                    methodInfo.CallingConvention,
                    methodInfo.ReturnType,
                    methodInfo.ReturnParameter.GetRequiredCustomModifiers(),
                    methodInfo.ReturnParameter.GetOptionalCustomModifiers(),
                    parameters,
                    parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
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
                    for (var i = 0; i < parameters.Length; i++)
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
            foreach (var typeR in s_normalTypes)
                if (typeR == type)
                    return true;
            return false;
        }
        private static readonly List<Type> s_normalTypes = new()
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
            var privateFieldName = GetPrivateFieldForPropertyName(property.Name);

            if (!createdNames.ContainsKey(privateFieldName))
            {
                if (property.GetMethod == null)
                    return;
                var getParameters = property.GetMethod!.GetParameters().Select(x => x.ParameterType).ToArray();

                var isIndexer = getParameters.Length > 0;
                var propertyType = !isIndexer ? property.PropertyType : typeof(Dictionary<,>).MakeGenericType(typeof(string), property.PropertyType);
                var attributes = FieldAttributes.Private;
                if (property.SetMethod != null && !property.CanWrite)
                    attributes |= FieldAttributes.InitOnly;
                var privateFieldBuilder = typeBuilder.DefineField(privateFieldName, propertyType, attributes);
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
                var getMethodBuilder = CreateMethod(property.GetMethod, typeBuilder, createdNames, (generator) =>
                {
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldfld, privateFieldBuilder);
                    if (isIndexer)
                    {
                        for (var i = 0; i < getParameters.Length; i++)
                            generator.Emit(OpCodes.Ldarg_S, i + 1);
                        generator.Emit(OpCodes.Callvirt, typeof(MockedAssembly).GetMethod(nameof(GetFromDictionary), BindingFlags.NonPublic | BindingFlags.Static)!);
                    }
                });
                propertyBuilder.SetGetMethod(getMethodBuilder);

                if (property.SetMethod != null)
                {
                    var setParameters = property.SetMethod.GetParameters().Select(x => x.ParameterType).ToArray();
                    var setMethodBuilder = CreateMethod(property.SetMethod, typeBuilder, createdNames,
                        (generator) =>
                        {
                            generator.Emit(OpCodes.Ldarg_0);
                            generator.Emit(OpCodes.Ldarg_1);
                            if (isIndexer)
                            {
                                for (var i = 0; i < setParameters.Length; i++)
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
                var type = entity.GetType();
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
