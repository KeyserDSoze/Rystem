using System.Reflection;
using System.Reflection.Emit;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ProxyService
    {
        private static readonly ModuleBuilder s_builder;
        static ProxyService()
        {
            var assemblyName = new AssemblyName($"Rystem.Proxy.Assembly.{Guid.NewGuid():N}");
            assemblyName.SetPublicKey(Assembly.GetExecutingAssembly().GetName().GetPublicKey());
            var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            s_builder = assembly.DefineDynamicModule(assemblyName.Name!);
        }
        public static (Type Interface, Type Implementation) AddProxy(
            this IServiceCollection services,
            Type interfaceType,
            Type implementationType,
            string interfaceName,
            string className,
            ServiceLifetime lifetime,
            params Type[] furtherInterfaces)
        {
            var interfaceTypeBuilder = s_builder.DefineType(interfaceName, TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.AnsiClass | TypeAttributes.AutoClass | TypeAttributes.Public);
            var newInterfaceType = interfaceTypeBuilder.CreateType();
            var typeBuilder = s_builder.DefineType(className, TypeAttributes.NotPublic | TypeAttributes.Class | TypeAttributes.Sealed);
            typeBuilder.AddInterfaceImplementation(newInterfaceType);
            var parentType = typeof(ProxyService<>).MakeGenericType(interfaceType);
            typeBuilder.SetParent(parentType);
            foreach (var furtherInterface in furtherInterfaces)
                typeBuilder.AddInterfaceImplementation(furtherInterface);
            var fieldInfo = parentType.GetField("_proxy", BindingFlags.NonPublic | BindingFlags.Instance);
            var fieldInfoParameters = parentType.GetField("_parameters", BindingFlags.NonPublic | BindingFlags.Instance);
            var fieldType = parentType.GetField("_proxyType", BindingFlags.NonPublic | BindingFlags.Instance);
            var constructors = implementationType.GetConstructors();
            if (constructors.Length == 0)
            {
                throw new InvalidOperationException($"{implementationType.Name} has no suitable public constructor.");
            }
            else
            {
                foreach (var constructor in constructors.Where(x => x != null))
                {
                    var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, constructor?.GetParameters().Select(x => x.ParameterType).ToArray() ?? Type.EmptyTypes);
                    var constructorGenerator = constructorBuilder.GetILGenerator();
                    constructorGenerator.Emit(OpCodes.Ldarg_0);
                    var localArrayOfObject = constructorGenerator.DeclareLocal(typeof(object[]));
                    if (!implementationType.IsPublic)
                    {
                        constructorGenerator.Emit(OpCodes.Ldc_I4, constructor!.GetParameters().Length);
                        constructorGenerator.Emit(OpCodes.Newarr, typeof(object));
                        constructorGenerator.Emit(OpCodes.Stloc, localArrayOfObject.LocalIndex);
                    }
                    var counter = 1;
                    foreach (var parameterType in constructor!.GetParameters().Select(x => x.ParameterType))
                    {
                        var parameterBuilder = constructorBuilder.DefineParameter(counter, ParameterAttributes.HasDefault, parameterType.Name);
                        SetCustomAttribute(parameterType.GetCustomAttributesData(), parameterBuilder.SetCustomAttribute);
                        if (implementationType.IsPublic)
                            constructorGenerator.Emit(OpCodes.Ldarg, counter++);
                        else
                        {
                            constructorGenerator.Emit(OpCodes.Ldloc, localArrayOfObject.LocalIndex);
                            constructorGenerator.Emit(OpCodes.Ldc_I4, counter - 1);
                            constructorGenerator.Emit(OpCodes.Ldarg, counter++);
                            if (parameterType.IsValueType)
                                constructorGenerator.Emit(OpCodes.Box, parameterType);
                            constructorGenerator.Emit(OpCodes.Stelem_Ref);
                        }
                    }
                    if (implementationType.IsPublic)
                    {
                        constructorGenerator.Emit(OpCodes.Newobj, constructor);
                        constructorGenerator.Emit(OpCodes.Castclass, interfaceType);
                        constructorGenerator.Emit(OpCodes.Stfld, fieldInfo!);
                    }
                    else
                    {
                        constructorGenerator.Emit(OpCodes.Ldloc, localArrayOfObject.LocalIndex);
                        constructorGenerator.Emit(OpCodes.Stfld, fieldInfoParameters!);
                        constructorGenerator.Emit(OpCodes.Ldarg_0);
                        constructorGenerator.Emit(OpCodes.Ldtoken, implementationType);
                        constructorGenerator.EmitWriteLine("call class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)");
                        constructorGenerator.Emit(OpCodes.Stfld, fieldType);
                    }
                    constructorGenerator.Emit(OpCodes.Ret);
                    SetCustomAttribute(constructor.GetCustomAttributesData(), constructorBuilder.SetCustomAttribute);
                }
                SetCustomAttribute(implementationType.GetCustomAttributesData(), typeBuilder.SetCustomAttribute);
            }
            var newType = typeBuilder.CreateType();
            var attributes = newType.GetCustomAttributes().ToList();
            var consAttributes = newType.GetConstructors().First().GetCustomAttributes().ToList();
            var parametersAttributes = newType.GetConstructors().First().GetParameters().SelectMany(x => x.GetCustomAttributes()).ToList();
            services.Add(new ServiceDescriptor(newInterfaceType, newType, lifetime));
            return (newInterfaceType, newType);
        }
        private static void SetCustomAttribute(
            IList<CustomAttributeData> attributes,
            Action<CustomAttributeBuilder> setter)
        {
            foreach (var attribute in attributes)
            {
                List<object> values = new();
                foreach (var attributeValue in attribute.ConstructorArguments)
                {
                    values.Add(attributeValue.Value!);
                }
                var attributeBuilder = new CustomAttributeBuilder(
                            attribute.Constructor, values.ToArray());
                setter(attributeBuilder);
            }
        }
    }
}
