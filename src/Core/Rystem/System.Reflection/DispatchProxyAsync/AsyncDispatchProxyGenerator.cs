using System.Reflection.Emit;
using System.Runtime.ExceptionServices;

namespace System.Reflection
{
    internal static class AsyncDispatchProxyGenerator
    {
        private const int InvokeActionFieldAndCtorParameterIndex = 0;
        private static readonly Dictionary<Type, Dictionary<Type, Type>> s_baseTypeAndInterfaceToGeneratedProxyType = new();

        private static readonly ProxyAssembly s_proxyAssembly = new();
        private static readonly MethodInfo s_dispatchProxyInvokeMethod = typeof(DispatchProxyAsync).GetTypeInfo().GetDeclaredMethod("Invoke")!;
        private static readonly MethodInfo s_dispatchProxyInvokeTMethod = typeof(DispatchProxyAsync).GetTypeInfo().GetDeclaredMethod("InvokeT")!;
        private static readonly MethodInfo s_dispatchProxyInvokeAsyncMethod = typeof(DispatchProxyAsync).GetTypeInfo().GetDeclaredMethod("InvokeAsync")!;
        private static readonly MethodInfo s_dispatchProxyInvokeAsyncTMethod = typeof(DispatchProxyAsync).GetTypeInfo().GetDeclaredMethod("InvokeAsyncT")!;
        internal static object CreateProxyInstance(Type baseType, Type interfaceType)
        {
            var proxiedType = GetProxyType(baseType, interfaceType);
            return Activator.CreateInstance(proxiedType, new DispatchProxyHandler())!;
        }

        private static Type GetProxyType(Type baseType, Type interfaceType)
        {
            lock (s_baseTypeAndInterfaceToGeneratedProxyType)
            {
                if (!s_baseTypeAndInterfaceToGeneratedProxyType.TryGetValue(baseType, out var interfaceToProxy))
                {
                    interfaceToProxy = new Dictionary<Type, Type>();
                    s_baseTypeAndInterfaceToGeneratedProxyType[baseType] = interfaceToProxy;
                }
                if (!interfaceToProxy.TryGetValue(interfaceType, out var generatedProxy))
                {
                    generatedProxy = GenerateProxyType(baseType, interfaceType);
                    interfaceToProxy[interfaceType] = generatedProxy;
                }
                return generatedProxy;
            }
        }
        private static Type GenerateProxyType(Type baseType, Type interfaceType)
        {
            var baseTypeInfo = baseType.GetTypeInfo();
            if (!interfaceType.GetTypeInfo().IsInterface)
            {
                throw new ArgumentException($"InterfaceType_Must_Be_Interface, {interfaceType.FullName}", "T");
            }
            if (baseTypeInfo.IsSealed)
            {
                throw new ArgumentException($"BaseType_Cannot_Be_Sealed, {baseTypeInfo.FullName}", "TProxy");
            }
            if (baseTypeInfo.IsAbstract)
            {
                throw new ArgumentException($"BaseType_Cannot_Be_Abstract {baseType.FullName}", "TProxy");
            }
            if (!baseTypeInfo.DeclaredConstructors.Any(c => c.IsPublic && c.GetParameters().Length == 0))
            {
                throw new ArgumentException($"BaseType_Must_Have_Default_Ctor {baseType.FullName}", "TProxy");
            }
            var pb = s_proxyAssembly.CreateProxy("generatedProxy", baseType);
            foreach (var t in interfaceType.GetTypeInfo().ImplementedInterfaces)
                pb.AddInterfaceImpl(t);
            pb.AddInterfaceImpl(interfaceType);
            var generatedProxyType = pb.CreateType();
            return generatedProxyType;
        }

        private sealed class ProxyMethodResolverContext
        {
            public PackedArgs Packed { get; }
            public MethodBase Method { get; }
            public ProxyMethodResolverContext(PackedArgs packed, MethodBase method)
            {
                Packed = packed;
                Method = method;
            }
        }
        private static ProxyMethodResolverContext Resolve(object[] args)
        {
            var packed = new PackedArgs(args);
            var method = s_proxyAssembly.ResolveMethodToken(packed.DeclaringType, packed.MethodToken);
            if (method.IsGenericMethodDefinition)
                method = ((MethodInfo)method).MakeGenericMethod(packed.GenericTypes);
            return new ProxyMethodResolverContext(packed, method);
        }

        public static void Invoke(object[] args)
        {
            var context = Resolve(args);
            try
            {
                _ = s_dispatchProxyInvokeMethod.Invoke(context.Packed.DispatchProxy, new object[] { context.Method, context.Packed.Args })!;
            }
            catch (TargetInvocationException tie)
            {
                ExceptionDispatchInfo.Capture(tie.InnerException!).Throw();
            }
        }
        public static T Invoke<T>(object[] args)
        {
            var context = Resolve(args);

            var returnValue = default(T);
            try
            {
                returnValue = (T)s_dispatchProxyInvokeTMethod.MakeGenericMethod(typeof(T))
                    .Invoke(context.Packed.DispatchProxy, new object[] { context.Method, context.Packed.Args })!;
                context.Packed.ReturnValue = returnValue;
            }
            catch (TargetInvocationException tie)
            {
                ExceptionDispatchInfo.Capture(tie.InnerException!).Throw();
            }

            return returnValue;
        }
        public static async Task InvokeAsync(object[] args)
        {
            var context = Resolve(args);
            try
            {
                await (Task)s_dispatchProxyInvokeAsyncMethod.Invoke(context.Packed.DispatchProxy, new object[] { context.Method, context.Packed.Args })!;
            }
            catch (TargetInvocationException tie)
            {
                ExceptionDispatchInfo.Capture(tie.InnerException!).Throw();
            }
        }

        public static async Task<T> InvokeAsync<T>(object[] args)
        {
            var context = Resolve(args);
            var returnValue = default(T);
            try
            {
                var genericmethod = s_dispatchProxyInvokeAsyncTMethod.MakeGenericMethod(typeof(T));
                returnValue = await (Task<T>)genericmethod.Invoke(context.Packed.DispatchProxy, new object[] { context.Method, context.Packed.Args })!;
                context.Packed.ReturnValue = returnValue!;
            }
            catch (TargetInvocationException tie)
            {
                ExceptionDispatchInfo.Capture(tie.InnerException!).Throw();
            }
            return returnValue;
        }
        private sealed class PackedArgs
        {
            internal const int DispatchProxyPosition = 0;
            internal const int DeclaringTypePosition = 1;
            internal const int MethodTokenPosition = 2;
            internal const int ArgsPosition = 3;
            internal const int GenericTypesPosition = 4;
            internal const int ReturnValuePosition = 5;

            internal static readonly Type[] PackedTypes = new Type[] { typeof(object), typeof(Type), typeof(int), typeof(object[]), typeof(Type[]), typeof(object) };

            private object[] _args;
            internal PackedArgs() : this(new object[PackedTypes.Length]) { }
            internal PackedArgs(object[] args) { _args = args; }

            internal DispatchProxyAsync DispatchProxy { get { return (DispatchProxyAsync)_args[DispatchProxyPosition]; } }
            internal Type DeclaringType { get { return (Type)_args[DeclaringTypePosition]; } }
            internal int MethodToken { get { return (int)_args[MethodTokenPosition]; } }
            internal object[] Args { get { return (object[])_args[ArgsPosition]; } }
            internal Type[] GenericTypes { get { return (Type[])_args[GenericTypesPosition]; } }
            internal object ReturnValue { set { _args[ReturnValuePosition] = value; } }
        }
        private sealed class ProxyAssembly
        {
            public AssemblyBuilder _ab;
            private ModuleBuilder _mb;
            private int _typeId = 0;
            // to pass methods by token
            private Dictionary<MethodBase, int> _methodToToken = new();
            private List<MethodBase> _methodsByToken = new();
            private HashSet<string> _ignoresAccessAssemblyNames = new();
            private ConstructorInfo _ignoresAccessChecksToAttributeConstructor;

            public ProxyAssembly()
            {
                AssemblyBuilderAccess access = AssemblyBuilderAccess.Run;
                var assemblyName = new AssemblyName("ProxyBuilder2");
                assemblyName.Version = new Version(1, 0, 0);
                _ab = AssemblyBuilder.DefineDynamicAssembly(assemblyName, access);
                _mb = _ab.DefineDynamicModule("testmod");
            }
            internal ConstructorInfo IgnoresAccessChecksAttributeConstructor
            {
                get
                {
                    if (_ignoresAccessChecksToAttributeConstructor == null)
                    {
                        TypeInfo attributeTypeInfo = GenerateTypeInfoOfIgnoresAccessChecksToAttribute();
                        _ignoresAccessChecksToAttributeConstructor = attributeTypeInfo.DeclaredConstructors.Single();
                    }

                    return _ignoresAccessChecksToAttributeConstructor;
                }
            }
            public ProxyBuilder CreateProxy(string name, Type proxyBaseType)
            {
                int nextId = Interlocked.Increment(ref _typeId);
                TypeBuilder tb = _mb.DefineType(name + "_" + nextId, TypeAttributes.Public, proxyBaseType);
                return new ProxyBuilder(this, tb, proxyBaseType);
            }
            private TypeInfo GenerateTypeInfoOfIgnoresAccessChecksToAttribute()
            {
                var attributeTypeBuilder =
                    _mb.DefineType("System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute",
                                   TypeAttributes.Public | TypeAttributes.Class,
                                   typeof(Attribute));

                var assemblyNameField =
                    attributeTypeBuilder.DefineField("assemblyName", typeof(String), FieldAttributes.Private);
                var constructorBuilder = attributeTypeBuilder.DefineConstructor(MethodAttributes.Public,
                                                             CallingConventions.HasThis,
                                                             new Type[] { assemblyNameField.FieldType });
                var il = constructorBuilder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg, 1);
                il.Emit(OpCodes.Stfld, assemblyNameField);
                il.Emit(OpCodes.Ret);
                var getterPropertyBuilder = attributeTypeBuilder.DefineProperty(
                                                       "AssemblyName",
                                                       PropertyAttributes.None,
                                                       CallingConventions.HasThis,
                                                       returnType: typeof(String),
                                                       parameterTypes: null);
                var getterMethodBuilder = attributeTypeBuilder.DefineMethod(
                                                       "get_AssemblyName",
                                                       MethodAttributes.Public,
                                                       CallingConventions.HasThis,
                                                       returnType: typeof(String),
                                                       parameterTypes: null);
                il = getterMethodBuilder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, assemblyNameField);
                il.Emit(OpCodes.Ret);
                var attributeUsageTypeInfo = typeof(AttributeUsageAttribute).GetTypeInfo();
                var attributeUsageConstructorInfo =
                    attributeUsageTypeInfo.DeclaredConstructors
                        .Single(c => c.GetParameters().Count() == 1 &&
                                     c.GetParameters()[0].ParameterType == typeof(AttributeTargets));
                var allowMultipleProperty =
                    attributeUsageTypeInfo.DeclaredProperties
                        .Single(f => String.Equals(f.Name, "AllowMultiple"));
                var customAttributeBuilder =
                    new CustomAttributeBuilder(attributeUsageConstructorInfo,
                                                new object[] { AttributeTargets.Assembly },
                                                new PropertyInfo[] { allowMultipleProperty },
                                                new object[] { true });
                attributeTypeBuilder.SetCustomAttribute(customAttributeBuilder);
                return attributeTypeBuilder.CreateTypeInfo();
            }
            internal void GenerateInstanceOfIgnoresAccessChecksToAttribute(string assemblyName)
            {
                var attributeConstructor = IgnoresAccessChecksAttributeConstructor;
                var customAttributeBuilder =
                    new CustomAttributeBuilder(attributeConstructor, new object[] { assemblyName });
                _ab.SetCustomAttribute(customAttributeBuilder);
            }
            internal void EnsureTypeIsVisible(Type type)
            {
                var typeInfo = type.GetTypeInfo();
                if (!typeInfo.IsVisible)
                {
                    var assemblyName = typeInfo.Assembly.GetName().Name;
                    if (!_ignoresAccessAssemblyNames.Contains(assemblyName!))
                    {
                        GenerateInstanceOfIgnoresAccessChecksToAttribute(assemblyName!);
                        _ignoresAccessAssemblyNames.Add(assemblyName!);
                    }
                }
            }

            internal void GetTokenForMethod(MethodBase method, out Type type, out int token)
            {
                type = method.DeclaringType;
                token = 0;
                if (!_methodToToken.TryGetValue(method, out token))
                {
                    _methodsByToken.Add(method);
                    token = _methodsByToken.Count - 1;
                    _methodToToken[method] = token;
                }
            }
            internal MethodBase ResolveMethodToken(Type type, int token)
            {
                return _methodsByToken[token];
            }
        }

        private sealed class ProxyBuilder
        {
            private static readonly MethodInfo s_delegateInvoke = typeof(DispatchProxyHandler).GetMethod("InvokeHandle")!;
            private static readonly MethodInfo s_delegateInvokeT = typeof(DispatchProxyHandler).GetMethod("InvokeHandleT")!;
            private static readonly MethodInfo s_delegateInvokeAsync = typeof(DispatchProxyHandler).GetMethod("InvokeAsyncHandle")!;
            private static readonly MethodInfo s_delegateinvokeAsyncT = typeof(DispatchProxyHandler).GetMethod("InvokeAsyncHandleT")!;

            private ProxyAssembly _assembly;
            private TypeBuilder _tb;
            private Type _proxyBaseType;
            private List<FieldBuilder> _fields;

            internal ProxyBuilder(ProxyAssembly assembly, TypeBuilder tb, Type proxyBaseType)
            {
                _assembly = assembly;
                _tb = tb;
                _proxyBaseType = proxyBaseType;
                _fields = new List<FieldBuilder>();
                _fields.Add(tb.DefineField("_handler", typeof(DispatchProxyHandler), FieldAttributes.Private));
            }

            private static bool IsGenericTask(Type type)
            {
                var current = type;
                while (current != null)
                {
                    if (current.GetTypeInfo().IsGenericType && current.GetGenericTypeDefinition() == typeof(Task<>))
                        return true;
                    current = current.GetTypeInfo().BaseType;
                }
                return false;
            }

            private void Complete()
            {
                var args = new Type[_fields.Count];
                for (var i = 0; i < args.Length; i++)
                {
                    args[i] = _fields[i].FieldType;
                }
                var cb = _tb.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, args);
                var il = cb.GetILGenerator();
                var baseCtor = _proxyBaseType.GetTypeInfo().DeclaredConstructors.SingleOrDefault(c => c.IsPublic && c.GetParameters().Length == 0);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, baseCtor);
                for (int i = 0; i < args.Length; i++)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg, i + 1);
                    il.Emit(OpCodes.Stfld, _fields[i]);
                }
                il.Emit(OpCodes.Ret);
            }

            internal Type CreateType()
            {
                this.Complete();
                return _tb.CreateTypeInfo().AsType();
            }

            internal void AddInterfaceImpl(Type iface)
            {
                _assembly.EnsureTypeIsVisible(iface);
                _tb.AddInterfaceImplementation(iface);
                var propertyMap = new Dictionary<MethodInfo, PropertyAccessorInfo>(MethodInfoEqualityComparer.Instance);
                foreach (var pi in iface.GetRuntimeProperties())
                {
                    var ai = new PropertyAccessorInfo(pi.GetMethod!, pi.SetMethod!);
                    if (pi.GetMethod != null)
                        propertyMap[pi.GetMethod] = ai;
                    if (pi.SetMethod != null)
                        propertyMap[pi.SetMethod] = ai;
                }
                var eventMap = new Dictionary<MethodInfo, EventAccessorInfo>(MethodInfoEqualityComparer.Instance);
                foreach (var ei in iface.GetRuntimeEvents())
                {
                    var ai = new EventAccessorInfo(ei.AddMethod!, ei.RemoveMethod!, ei.RaiseMethod!);
                    if (ei.AddMethod != null)
                        eventMap[ei.AddMethod] = ai;
                    if (ei.RemoveMethod != null)
                        eventMap[ei.RemoveMethod] = ai;
                    if (ei.RaiseMethod != null)
                        eventMap[ei.RaiseMethod] = ai;
                }

                foreach (var mi in iface.GetRuntimeMethods())
                {
                    var mdb = AddMethodImpl(mi);
                    if (propertyMap.TryGetValue(mi, out var associatedProperty))
                    {
                        if (MethodInfoEqualityComparer.Instance.Equals(associatedProperty.InterfaceGetMethod, mi))
                            associatedProperty.GetMethodBuilder = mdb;
                        else
                            associatedProperty.SetMethodBuilder = mdb;
                    }
                    if (eventMap.TryGetValue(mi, out var associatedEvent))
                    {
                        if (MethodInfoEqualityComparer.Instance.Equals(associatedEvent.InterfaceAddMethod, mi))
                            associatedEvent.AddMethodBuilder = mdb;
                        else if (MethodInfoEqualityComparer.Instance.Equals(associatedEvent.InterfaceRemoveMethod, mi))
                            associatedEvent.RemoveMethodBuilder = mdb;
                        else
                            associatedEvent.RaiseMethodBuilder = mdb;
                    }
                }
                foreach (var pi in iface.GetRuntimeProperties())
                {
                    var ai = propertyMap[pi.GetMethod ?? pi.SetMethod];
                    var pb = _tb.DefineProperty(pi.Name, pi.Attributes, pi.PropertyType, pi.GetIndexParameters().Select(p => p.ParameterType).ToArray());
                    if (ai.GetMethodBuilder != null)
                        pb.SetGetMethod(ai.GetMethodBuilder);
                    if (ai.SetMethodBuilder != null)
                        pb.SetSetMethod(ai.SetMethodBuilder);
                }

                foreach (var ei in iface.GetRuntimeEvents())
                {
                    var ai = eventMap[ei.AddMethod ?? ei.RemoveMethod];
                    var eb = _tb.DefineEvent(ei.Name, ei.Attributes, ei.EventHandlerType);
                    if (ai.AddMethodBuilder != null)
                        eb.SetAddOnMethod(ai.AddMethodBuilder);
                    if (ai.RemoveMethodBuilder != null)
                        eb.SetRemoveOnMethod(ai.RemoveMethodBuilder);
                    if (ai.RaiseMethodBuilder != null)
                        eb.SetRaiseMethod(ai.RaiseMethodBuilder);
                }
            }

            private MethodBuilder AddMethodImpl(MethodInfo mi)
            {
                var parameters = mi.GetParameters();
                var paramTypes = ParamTypes(parameters, false);

                var mdb = _tb.DefineMethod(mi.Name, MethodAttributes.Public | MethodAttributes.Virtual, mi.ReturnType, paramTypes);
                if (mi.ContainsGenericParameters)
                {
                    var ts = mi.GetGenericArguments();
                    var ss = new string[ts.Length];
                    for (var i = 0; i < ts.Length; i++)
                    {
                        ss[i] = ts[i].Name;
                    }
                    var genericParameters = mdb.DefineGenericParameters(ss);
                    for (var i = 0; i < genericParameters.Length; i++)
                    {
                        genericParameters[i].SetGenericParameterAttributes(ts[i].GetTypeInfo().GenericParameterAttributes);
                    }
                }
                var il = mdb.GetILGenerator();
                var args = new ParametersArray(il, paramTypes);
                il.Emit(OpCodes.Nop);
                var argsArr = new GenericArray<object>(il, ParamTypes(parameters, true).Length);
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (!parameters[i].IsOut)
                    {
                        argsArr.BeginSet(i);
                        args.Get(i);
                        argsArr.EndSet(parameters[i].ParameterType);
                    }
                }
                var packedArr = new GenericArray<object>(il, PackedArgs.PackedTypes.Length);
                packedArr.BeginSet(PackedArgs.DispatchProxyPosition);
                il.Emit(OpCodes.Ldarg_0);
                packedArr.EndSet(typeof(DispatchProxyAsync));
                var Type_GetTypeFromHandle = typeof(Type).GetRuntimeMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) });
                Type declaringType;
                _assembly.GetTokenForMethod(mi, out declaringType, out var methodToken);
                packedArr.BeginSet(PackedArgs.DeclaringTypePosition);
                il.Emit(OpCodes.Ldtoken, declaringType);
                il.Emit(OpCodes.Call, Type_GetTypeFromHandle!);
                packedArr.EndSet(typeof(object));
                packedArr.BeginSet(PackedArgs.MethodTokenPosition);
                il.Emit(OpCodes.Ldc_I4, methodToken);
                packedArr.EndSet(typeof(Int32));
                packedArr.BeginSet(PackedArgs.ArgsPosition);
                argsArr.Load();
                packedArr.EndSet(typeof(object[]));
                if (mi.ContainsGenericParameters)
                {
                    packedArr.BeginSet(PackedArgs.GenericTypesPosition);
                    var genericTypes = mi.GetGenericArguments();
                    var typeArr = new GenericArray<Type>(il, genericTypes.Length);
                    for (var i = 0; i < genericTypes.Length; ++i)
                    {
                        typeArr.BeginSet(i);
                        il.Emit(OpCodes.Ldtoken, genericTypes[i]);
                        il.Emit(OpCodes.Call, Type_GetTypeFromHandle);
                        typeArr.EndSet(typeof(Type));
                    }
                    typeArr.Load();
                    packedArr.EndSet(typeof(Type[]));
                }

                for (var i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType.IsByRef)
                    {
                        args.BeginSet(i);
                        argsArr.Get(i);
                        args.EndSet(i, typeof(object));
                    }
                }
                x
                var invokeMethod = s_delegateInvoke;
                if (mi.ReturnType == typeof(Task))
                {
                    invokeMethod = s_delegateInvokeAsync;
                }
                if (IsGenericTask(mi.ReturnType))
                {
                    var returnTypes = mi.ReturnType.GetGenericArguments();
                    invokeMethod = s_delegateinvokeAsyncT.MakeGenericMethod(returnTypes);
                }
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, _fields[InvokeActionFieldAndCtorParameterIndex]);
                packedArr.Load();
                il.Emit(OpCodes.Callvirt, invokeMethod);
                if (mi.ReturnType != typeof(void))
                {
                    Convert(il, typeof(object), mi.ReturnType, false);
                }
                else
                {
                    il.Emit(OpCodes.Pop);
                }
                il.Emit(OpCodes.Ret);
                _tb.DefineMethodOverride(mdb, mi);
                return mdb;
            }

            private static Type[] ParamTypes(ParameterInfo[] parms, bool noByRef)
            {
                var types = new Type[parms.Length];
                for (var i = 0; i < parms.Length; i++)
                {
                    types[i] = parms[i].ParameterType;
                    if (noByRef && types[i].IsByRef)
                        types[i] = types[i].GetElementType()!;
                }
                return types;
            }
            private static int GetTypeCode(Type type)
            {
                if (type == null)
                    return 0;   // TypeCode.Empty;

                if (type == typeof(Boolean))
                    return 3;   // TypeCode.Boolean;

                if (type == typeof(Char))
                    return 4;   // TypeCode.Char;

                if (type == typeof(SByte))
                    return 5;   // TypeCode.SByte;

                if (type == typeof(Byte))
                    return 6;   // TypeCode.Byte;

                if (type == typeof(Int16))
                    return 7;   // TypeCode.Int16;

                if (type == typeof(UInt16))
                    return 8;   // TypeCode.UInt16;

                if (type == typeof(Int32))
                    return 9;   // TypeCode.Int32;

                if (type == typeof(UInt32))
                    return 10;  // TypeCode.UInt32;

                if (type == typeof(Int64))
                    return 11;  // TypeCode.Int64;

                if (type == typeof(UInt64))
                    return 12;  // TypeCode.UInt64;

                if (type == typeof(Single))
                    return 13;  // TypeCode.Single;

                if (type == typeof(Double))
                    return 14;  // TypeCode.Double;

                if (type == typeof(Decimal))
                    return 15;  // TypeCode.Decimal;

                if (type == typeof(DateTime))
                    return 16;  // TypeCode.DateTime;

                if (type == typeof(String))
                    return 18;  // TypeCode.String;

                if (type.GetTypeInfo().IsEnum)
                    return GetTypeCode(Enum.GetUnderlyingType(type));

                return 1;   // TypeCode.Object;
            }

            private static readonly OpCode[] s_convOpCodes = new OpCode[] {
                OpCodes.Nop,//Empty = 0,
                OpCodes.Nop,//Object = 1,
                OpCodes.Nop,//DBNull = 2,
                OpCodes.Conv_I1,//Boolean = 3,
                OpCodes.Conv_I2,//Char = 4,
                OpCodes.Conv_I1,//SByte = 5,
                OpCodes.Conv_U1,//Byte = 6,
                OpCodes.Conv_I2,//Int16 = 7,
                OpCodes.Conv_U2,//UInt16 = 8,
                OpCodes.Conv_I4,//Int32 = 9,
                OpCodes.Conv_U4,//UInt32 = 10,
                OpCodes.Conv_I8,//Int64 = 11,
                OpCodes.Conv_U8,//UInt64 = 12,
                OpCodes.Conv_R4,//Single = 13,
                OpCodes.Conv_R8,//Double = 14,
                OpCodes.Nop,//Decimal = 15,
                OpCodes.Nop,//DateTime = 16,
                OpCodes.Nop,//17
                OpCodes.Nop,//String = 18,
            };

            private static readonly OpCode[] s_ldindOpCodes = new OpCode[] {
                OpCodes.Nop,//Empty = 0,
                OpCodes.Nop,//Object = 1,
                OpCodes.Nop,//DBNull = 2,
                OpCodes.Ldind_I1,//Boolean = 3,
                OpCodes.Ldind_I2,//Char = 4,
                OpCodes.Ldind_I1,//SByte = 5,
                OpCodes.Ldind_U1,//Byte = 6,
                OpCodes.Ldind_I2,//Int16 = 7,
                OpCodes.Ldind_U2,//UInt16 = 8,
                OpCodes.Ldind_I4,//Int32 = 9,
                OpCodes.Ldind_U4,//UInt32 = 10,
                OpCodes.Ldind_I8,//Int64 = 11,
                OpCodes.Ldind_I8,//UInt64 = 12,
                OpCodes.Ldind_R4,//Single = 13,
                OpCodes.Ldind_R8,//Double = 14,
                OpCodes.Nop,//Decimal = 15,
                OpCodes.Nop,//DateTime = 16,
                OpCodes.Nop,//17
                OpCodes.Ldind_Ref,//String = 18,
            };

            private static readonly OpCode[] s_stindOpCodes = new OpCode[] {
                OpCodes.Nop,//Empty = 0,
                OpCodes.Nop,//Object = 1,
                OpCodes.Nop,//DBNull = 2,
                OpCodes.Stind_I1,//Boolean = 3,
                OpCodes.Stind_I2,//Char = 4,
                OpCodes.Stind_I1,//SByte = 5,
                OpCodes.Stind_I1,//Byte = 6,
                OpCodes.Stind_I2,//Int16 = 7,
                OpCodes.Stind_I2,//UInt16 = 8,
                OpCodes.Stind_I4,//Int32 = 9,
                OpCodes.Stind_I4,//UInt32 = 10,
                OpCodes.Stind_I8,//Int64 = 11,
                OpCodes.Stind_I8,//UInt64 = 12,
                OpCodes.Stind_R4,//Single = 13,
                OpCodes.Stind_R8,//Double = 14,
                OpCodes.Nop,//Decimal = 15,
                OpCodes.Nop,//DateTime = 16,
                OpCodes.Nop,//17
                OpCodes.Stind_Ref,//String = 18,
            };

            private static void Convert(ILGenerator il, Type source, Type target, bool isAddress)
            {
                if (target == source)
                    return;

                var sourceTypeInfo = source.GetTypeInfo();
                var targetTypeInfo = target.GetTypeInfo();

                if (source.IsByRef)
                {
                    var argType = source.GetElementType();
                    Ldind(il, argType!);
                    Convert(il, argType, target, isAddress);
                    return;
                }
                if (targetTypeInfo.IsValueType)
                {
                    if (sourceTypeInfo.IsValueType)
                    {
                        var opCode = s_convOpCodes[GetTypeCode(target)];
                        il.Emit(opCode);
                    }
                    else
                    {
                        il.Emit(OpCodes.Unbox, target);
                        if (!isAddress)
                            Ldind(il, target);
                    }
                }
                else if (targetTypeInfo.IsAssignableFrom(sourceTypeInfo))
                {
                    if (sourceTypeInfo.IsValueType || source.IsGenericParameter)
                    {
                        if (isAddress)
                            Ldind(il, source);
                        il.Emit(OpCodes.Box, source);
                    }
                }
                else
                {
                    if (target.IsGenericParameter)
                    {
                        il.Emit(OpCodes.Unbox_Any, target);
                    }
                    else
                    {
                        il.Emit(OpCodes.Castclass, target);
                    }
                }
            }

            private static void Ldind(ILGenerator il, Type type)
            {
                var opCode = s_ldindOpCodes[GetTypeCode(type)];
                if (!opCode.Equals(OpCodes.Nop))
                {
                    il.Emit(opCode);
                }
                else
                {
                    il.Emit(OpCodes.Ldobj, type);
                }
            }

            private static void Stind(ILGenerator il, Type type)
            {
                var opCode = s_stindOpCodes[GetTypeCode(type)];
                if (!opCode.Equals(OpCodes.Nop))
                {
                    il.Emit(opCode);
                }
                else
                {
                    il.Emit(OpCodes.Stobj, type);
                }
            }

            private sealed class ParametersArray
            {
                private readonly ILGenerator _il;
                private readonly Type[] _paramTypes;
                internal ParametersArray(ILGenerator il, Type[] paramTypes)
                {
                    _il = il;
                    _paramTypes = paramTypes;
                }

                internal void Get(int i)
                {
                    _il.Emit(OpCodes.Ldarg, i + 1);
                }

                internal void BeginSet(int i)
                {
                    _il.Emit(OpCodes.Ldarg, i + 1);
                }

                internal void EndSet(int i, Type stackType)
                {
                    var argType = _paramTypes[i].GetElementType();
                    Convert(_il, stackType, argType!, false);
                    Stind(_il, argType!);
                }
            }

            private sealed class GenericArray<T>
            {
                private readonly ILGenerator _il;
                private readonly LocalBuilder _lb;
                internal GenericArray(ILGenerator il, int len)
                {
                    _il = il;
                    _lb = il.DeclareLocal(typeof(T[]));

                    il.Emit(OpCodes.Ldc_I4, len);
                    il.Emit(OpCodes.Newarr, typeof(T));
                    il.Emit(OpCodes.Stloc, _lb);
                }
                internal void Load()
                {
                    _il.Emit(OpCodes.Ldloc, _lb);
                }
                internal void Get(int i)
                {
                    _il.Emit(OpCodes.Ldloc, _lb);
                    _il.Emit(OpCodes.Ldc_I4, i);
                    _il.Emit(OpCodes.Ldelem_Ref);
                }
                internal void BeginSet(int i)
                {
                    _il.Emit(OpCodes.Ldloc, _lb);
                    _il.Emit(OpCodes.Ldc_I4, i);
                }
                internal void EndSet(Type stackType)
                {
                    Convert(_il, stackType, typeof(T), false);
                    _il.Emit(OpCodes.Stelem_Ref);
                }
            }

            private sealed class PropertyAccessorInfo
            {
                public MethodInfo InterfaceGetMethod { get; }
                public MethodInfo InterfaceSetMethod { get; }
                public MethodBuilder GetMethodBuilder { get; set; }
                public MethodBuilder SetMethodBuilder { get; set; }

                public PropertyAccessorInfo(MethodInfo interfaceGetMethod, MethodInfo interfaceSetMethod)
                {
                    InterfaceGetMethod = interfaceGetMethod;
                    InterfaceSetMethod = interfaceSetMethod;
                }
            }

            private sealed class EventAccessorInfo
            {
                public MethodInfo InterfaceAddMethod { get; }
                public MethodInfo InterfaceRemoveMethod { get; }
                public MethodInfo InterfaceRaiseMethod { get; }
                public MethodBuilder AddMethodBuilder { get; set; }
                public MethodBuilder RemoveMethodBuilder { get; set; }
                public MethodBuilder RaiseMethodBuilder { get; set; }

                public EventAccessorInfo(MethodInfo interfaceAddMethod, MethodInfo interfaceRemoveMethod, MethodInfo interfaceRaiseMethod)
                {
                    InterfaceAddMethod = interfaceAddMethod;
                    InterfaceRemoveMethod = interfaceRemoveMethod;
                    InterfaceRaiseMethod = interfaceRaiseMethod;
                }
            }

            private sealed class MethodInfoEqualityComparer : EqualityComparer<MethodInfo>
            {
                public static readonly MethodInfoEqualityComparer Instance = new MethodInfoEqualityComparer();
                private MethodInfoEqualityComparer() { }
                public sealed override bool Equals(MethodInfo left, MethodInfo right)
                {
                    if (ReferenceEquals(left, right))
                        return true;
                    if (left == null)
                        return right == null;
                    else if (right == null)
                        return false;
                    if (!Equals(left.DeclaringType, right.DeclaringType))
                        return false;
                    if (!Equals(left.ReturnType, right.ReturnType))
                        return false;
                    if (left.CallingConvention != right.CallingConvention)
                        return false;
                    if (left.IsStatic != right.IsStatic)
                        return false;
                    if (left.Name != right.Name)
                        return false;
                    var leftGenericParameters = left.GetGenericArguments();
                    var rightGenericParameters = right.GetGenericArguments();
                    if (leftGenericParameters.Length != rightGenericParameters.Length)
                        return false;

                    for (var i = 0; i < leftGenericParameters.Length; i++)
                    {
                        if (!Equals(leftGenericParameters[i], rightGenericParameters[i]))
                            return false;
                    }

                    var leftParameters = left.GetParameters();
                    var rightParameters = right.GetParameters();
                    if (leftParameters.Length != rightParameters.Length)
                        return false;

                    for (var i = 0; i < leftParameters.Length; i++)
                    {
                        if (!Equals(leftParameters[i].ParameterType, rightParameters[i].ParameterType))
                            return false;
                    }

                    return true;
                }

                public sealed override int GetHashCode(MethodInfo obj)
                {
                    if (obj == null)
                        return 0;

                    var hashCode = obj.DeclaringType!.GetHashCode();
                    hashCode ^= obj.Name.GetHashCode();
                    foreach (var parameter in obj.GetParameters())
                    {
                        hashCode ^= parameter.ParameterType.GetHashCode();
                    }

                    return hashCode;
                }
            }
        }
    }
}
