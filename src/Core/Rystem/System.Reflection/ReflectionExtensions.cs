using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace System.Reflection
{
    public static class ReflectionExtensions
    {
        private static readonly Dictionary<string, PropertyInfo[]> AllProperties = new();
        private static readonly Dictionary<string, ConstructorInfo[]> AllConstructors = new();
        private static readonly Dictionary<string, FieldInfo[]> AllFields = new();
        private static readonly Dictionary<string, MethodInfo[]> AllMethods = new();
        private static readonly Dictionary<string, MethodInfo[]> AllStaticMethods = new();

        private static readonly object Semaphore = new();
        private static readonly Type ObjectType = typeof(object);
        /// <summary>
        /// Check if type is the same type or a son of toCompare.
        /// </summary>
        /// <param name="type">Type to check.</param>
        /// <param name="toCompare">Type to compare.</param>
        /// <returns>bool</returns>
        public static bool IsTheSameTypeOrASon(this Type type, Type toCompare)
        {
            if (toCompare == ObjectType)
                return true;
            if (type == ObjectType && toCompare == ObjectType)
                return true;
            while (type != null && type != ObjectType)
            {
                if (type == toCompare)
                    return true;
                type = type.BaseType!;
            }
            return false;
        }
        /// <summary>
        /// Check if type is the same type or a son of toCompare.
        /// </summary>
        /// <param name="type">Type to check.</param>
        /// <param name="toCompare">Type to compare.</param>
        /// <returns>bool</returns>
        public static bool IsTheSameTypeOrASon<TCompared>(this Type type, TCompared toCompare)
            => type.IsTheSameTypeOrASon(toCompare?.GetType() ?? typeof(TCompared));
        /// <summary>
        /// Check if type is the same type or a son of toCompare.
        /// </summary>
        /// <typeparam name="T">Type to check.</typeparam>
        /// <typeparam name="TCompared">Type to compare.</typeparam>
        /// <param name="item">Item to check.</param>
        /// <param name="toCompare">Item to compare.</param>
        /// <returns>bool</returns>
        public static bool IsTheSameTypeOrASon<T, TCompared>(this T item, TCompared toCompare)
            => (item?.GetType() ?? typeof(T)).IsTheSameTypeOrASon(toCompare?.GetType() ?? typeof(TCompared));
        /// <summary>
        /// Check if type is the same type or a father of toCompare.
        /// </summary>
        /// <param name="type">Type to check.</param>
        /// <param name="toCompare">Type to compare.</param>
        /// <returns>bool</returns>
        public static bool IsTheSameTypeOrAFather(this Type type, Type toCompare)
            => toCompare.IsTheSameTypeOrASon(type);
        /// <summary>
        /// Check if type is the same type or a father of toCompare.
        /// </summary>
        /// <typeparam name="T">Type to check.</typeparam>
        /// <typeparam name="TCompared">Type to compare.</typeparam>
        /// <param name="item">Item to check.</param>
        /// <param name="toCompare">Item to compare.</param>
        /// <returns>bool</returns>
        public static bool IsTheSameTypeOrAFather<T, TCompared>(this T item, TCompared toCompare)
            => (item?.GetType() ?? typeof(T)).IsTheSameTypeOrAFather(toCompare?.GetType() ?? typeof(TCompared));
        /// <summary>
        /// Check if type is the same type or a son/father of toCompare.
        /// </summary>
        /// <param name="type">Type to check.</param>
        /// <param name="toCompare">Type to compare.</param>
        /// <returns>bool</returns>
        public static bool IsTheSameTypeOrAParent(this Type type, Type toCompare)
            => type.IsTheSameTypeOrAFather(toCompare) || type.IsTheSameTypeOrASon(toCompare);
        /// <summary>
        /// Check if type is the same type or a son/father of toCompare.
        /// </summary>
        /// <typeparam name="T">Type to check.</typeparam>
        /// <typeparam name="TCompared">Type to compare.</typeparam>
        /// <param name="item">Item to check.</param>
        /// <param name="toCompare">Item to compare.</param>
        /// <returns></returns>
        public static bool IsTheSameTypeOrAParent<T, TCompared>(this T item, TCompared toCompare)
            => (item?.GetType() ?? typeof(T)).IsTheSameTypeOrAParent(toCompare?.GetType() ?? typeof(TCompared));

        public static bool HasTheSameTypeOrAParentInGeneric<T>(this T item, Type toCompare)
        {
            if (item == null)
                return false;
            foreach (var argument in item.GetType().GetGenericArguments())
            {
                if (argument.IsGenericParameter && argument.HasTheSameTypeOrAParentInGeneric(toCompare))
                    return true;
                else if (IsTheSameTypeOrASon(argument, toCompare) || IsTheSameTypeOrASon(toCompare, argument))
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Fetch all instance | public properties.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="attributesToIgnore">Attributes to ignore.</param>
        /// <returns>PropertyInfo[]</returns>
        public static PropertyInfo[] FetchProperties(this Type type, params Type[] attributesToIgnore)
        {
            if (!AllProperties.ContainsKey(type.FullName!))
                lock (Semaphore)
                    if (!AllProperties.ContainsKey(type.FullName!))
                        AllProperties.Add(type.FullName!, type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                            .Where(x =>
                            {
                                foreach (Type attributeToIgnore in attributesToIgnore)
                                    if (x.GetCustomAttribute(attributeToIgnore) != default)
                                        return false;
                                return true;
                            }).ToArray());
            return AllProperties[type.FullName!];
        }
        /// <summary>
        /// Fetch all constructors.
        /// </summary>
        /// <param name="type"></param>
        /// <returns>ConstructorInfo[]</returns>
        public static ConstructorInfo[] FecthConstructors(this Type type)
        {
            if (!AllConstructors.ContainsKey(type.FullName!))
                lock (Semaphore)
                    if (!AllConstructors.ContainsKey(type.FullName!))
                        AllConstructors.Add(type.FullName!, type.GetConstructors());
            return AllConstructors[type.FullName!];
        }
        /// <summary>
        /// Fetch all fields.
        /// </summary>
        /// <param name="type"></param>
        /// <returns>FieldInfo[]</returns>
        public static FieldInfo[] FetchFields(this Type type)
        {
            if (!AllFields.ContainsKey(type.FullName!))
                lock (Semaphore)
                    if (!AllFields.ContainsKey(type.FullName!))
                        AllFields.Add(type.FullName!, type.GetFields());
            return AllFields[type.FullName!];
        }
        /// <summary>
        /// Fetch all methods.
        /// </summary>
        /// <param name="type"></param>
        /// <returns>MethodInfo[]</returns>
        public static MethodInfo[] FetchMethods(this Type type)
        {
            if (!AllMethods.ContainsKey(type.FullName!))
                lock (Semaphore)
                    if (!AllMethods.ContainsKey(type.FullName!))
                        AllMethods.Add(type.FullName!, type.GetMethods());
            return AllMethods[type.FullName!];
        }
        /// <summary>
        /// Fetch all static methods.
        /// </summary>
        /// <param name="type"></param>
        /// <returns>MethodInfo[]</returns>
        public static MethodInfo[] FetchStaticMethods(this Type type)
        {
            if (!AllStaticMethods.ContainsKey(type.FullName!))
                lock (Semaphore)
                    if (!AllStaticMethods.ContainsKey(type.FullName!))
                        AllStaticMethods.Add(type.FullName!, type.GetMethods(BindingFlags.Public | BindingFlags.Static));
            return AllStaticMethods[type.FullName!];
        }
        private static readonly ConcurrentDictionary<string, Func<object?>> s_defaultCreators = new();
        /// <summary>
        /// Create an instance with default values.
        /// </summary>
        /// <param name="type"></param>
        /// <returns>object</returns>
        public static object? CreateWithDefault(this Type type)
        {
            if (!s_defaultCreators.ContainsKey(type.FullName!))
            {
                if (type.IsPrimitive())
                {
                    if (type == typeof(string))
                        s_defaultCreators.TryAdd(type.FullName!,
                        () => null);
                    else
                        s_defaultCreators.TryAdd(type.FullName!,
                            () => Activator.CreateInstance(type));
                }
                else if ((type.IsInterface || type.IsAbstract) && type.IsAssignableTo(typeof(IDictionary)))
                {
                    s_defaultCreators.TryAdd(type.FullName!,
                        () => Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(type.GetGenericArguments())));
                }
                else if ((type.IsInterface || type.IsAbstract) && type.IsAssignableTo(typeof(IEnumerable)))
                {
                    var generics = type.GetGenericArguments();
                    if (generics.Length > 0)
                    {
                        var currentType = typeof(List<>).MakeGenericType(generics);
                        s_defaultCreators.TryAdd(type.FullName!, () => Activator.CreateInstance(currentType));
                    }
                    else
                        s_defaultCreators.TryAdd(type.FullName!, () => new ArrayList());
                }
                else
                {
                    if (type.IsInterface || type.IsAbstract)
                        type = type.Mock()!;
                    var constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).OrderByDescending(x => x.GetParameters().Length).FirstOrDefault();
                    if (constructor != null)
                    {
                        var parameters = constructor.GetParameters();
                        if (parameters.Length > 0)
                            s_defaultCreators.TryAdd(type.FullName!,
                                () =>
                                {
                                    return Activator.CreateInstance(type, parameters.Select(x => x.ParameterType.CreateWithDefault()).ToArray());
                                });
                        else
                            s_defaultCreators.TryAdd(type.FullName!,
                                () => Activator.CreateInstance(type));
                    }
                    else
                        s_defaultCreators.TryAdd(type.FullName!,
                            () => Activator.CreateInstance(type));
                }
            }
            return s_defaultCreators[type.FullName!].Invoke();
        }
        /// <summary>
        /// Create an instance with default values.
        /// </summary>
        /// <param name="type"></param>
        /// <returns>T?</returns>
        public static T? CreateWithDefault<T>(this Type type)
        {
            var value = type.CreateWithDefault();
            if (value == null)
                return default;
            else
                return (T)value;
        }
        private static readonly ConcurrentDictionary<string, Func<object?>> s_defaultCreatorsWithConstructorPropertiesAndField = new();
        /// <summary>
        /// Create an instance with default values.
        /// </summary>
        /// <param name="type"></param>
        /// <returns>object</returns>
        public static object? CreateWithDefaultConstructorPropertiesAndField(this Type type)
        {
            if (!s_defaultCreatorsWithConstructorPropertiesAndField.ContainsKey(type.FullName!))
            {
                if (type.IsPrimitive() || type.IsAssignableTo(typeof(IDictionary)) || type.IsAssignableTo(typeof(IEnumerable)))
                {
                    s_defaultCreatorsWithConstructorPropertiesAndField.TryAdd(type.FullName!,
                        () => type.CreateWithDefault());
                }
                else
                {
                    List<PropertyInfo> properties = type
                            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                            .Where(x => x.SetMethod != null)
                            .ToList();
                    List<FieldInfo> fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).ToList();
                    s_defaultCreatorsWithConstructorPropertiesAndField.TryAdd(type.FullName!,
                        () =>
                        {
                            var entity = type.CreateWithDefault();
                            foreach (var property in properties)
                                property.SetValue(entity, property.PropertyType.CreateWithDefaultConstructorPropertiesAndField());
                            foreach (var field in fields)
                                field.SetValue(entity, field.FieldType.CreateWithDefaultConstructorPropertiesAndField());
                            return entity;
                        });
                }
            }
            return s_defaultCreatorsWithConstructorPropertiesAndField[type.FullName!].Invoke();
        }
        /// <summary>
        /// Create an instance with default values.
        /// </summary>
        /// <param name="type"></param>
        /// <returns>T?</returns>
        public static T? CreateWithDefaultConstructorPropertiesAndField<T>(this Type type)
        {
            var value = type.CreateWithDefaultConstructorPropertiesAndField();
            if (value == null)
                return default;
            else
                return (T)value;
        }
    }
}
