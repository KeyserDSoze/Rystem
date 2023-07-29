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
            //var fieldInfo = typeof(ProxyService<>).MakeGenericType(interfaceType).GetField($"<{nameof(ProxyService<object>.Proxy)}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            var fieldInfo = typeof(ProxyService<>).MakeGenericType(interfaceType).GetField("_proxy", BindingFlags.NonPublic | BindingFlags.Instance);
            var constructors = implementationType.GetConstructors();
            if (constructors.Length == 0)
            {
                var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
                var constructorGenerator = constructorBuilder.GetILGenerator();
                constructorGenerator.Emit(OpCodes.Ldarg_0);
                constructorGenerator.Emit(OpCodes.Newobj, implementationType);
                constructorGenerator.Emit(OpCodes.Castclass, interfaceType);
                constructorGenerator.Emit(OpCodes.Stfld, fieldInfo!);
                constructorGenerator.Emit(OpCodes.Ret);
            }
            else
            {
                foreach (var constructor in constructors.Where(x => x != null))
                {
                    var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, constructor?.GetParameters().Select(x => x.ParameterType).ToArray() ?? Type.EmptyTypes);
                    var constructorGenerator = constructorBuilder.GetILGenerator();
                    constructorGenerator.Emit(OpCodes.Ldarg_0);
                    var counter = 1;
                    foreach (var parameter in constructor!.GetParameters())
                        constructorGenerator.Emit(OpCodes.Ldarg, counter++);
                    constructorGenerator.Emit(OpCodes.Newobj, constructor);
                    constructorGenerator.Emit(OpCodes.Castclass, interfaceType);
                    constructorGenerator.Emit(OpCodes.Stfld, fieldInfo!);
                    constructorGenerator.Emit(OpCodes.Ret);
                }
            }

            var newType = typeBuilder.CreateType();
            services.Add(new ServiceDescriptor(newInterfaceType, newType, lifetime));
            return (newInterfaceType, newType);
        }
    }
}
