using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using SpanJson.Formatters;
using SpanJson.Helpers;

namespace SpanJson.Resolvers
{
    public abstract class IntegratedFormatterBuilder
    {

        internal static IJsonFormatter GetDefaultOrCreate(Type type)
        {
            return (IJsonFormatter) (type.GetField("Default", BindingFlags.Public | BindingFlags.Static)
                                         ?.GetValue(null) ?? Activator.CreateInstance(type)); // leave the createinstance here, this helps with recursive types
        }
    }

    public class IntegratedFormatterBuilder<TSymbol, TResolver> : IntegratedFormatterBuilder, IJsonFormatterBuilder<TSymbol, TResolver>
        where TResolver : IJsonFormatterResolver<TSymbol, TResolver>, new() where TSymbol : struct
    {
        private readonly SpanJsonOptions _spanJsonOptions;

        public IntegratedFormatterBuilder(SpanJsonOptions spanJsonOptions)
        {
            _spanJsonOptions = spanJsonOptions;
        }

        private static bool HasCustomFormatterForRelatedType(Type type)
        {
            Type relatedType = Nullable.GetUnderlyingType(type);
            if (relatedType == null && type.IsArray)
            {
                relatedType = type.GetElementType();
            }

            if (relatedType == null && type.TryGetTypeOfGenericInterface(typeof(IList<>), out var argumentTypes) && argumentTypes.Length == 1)
            {
                relatedType = argumentTypes.Single();
            }

            if (relatedType != null)
            {
                if (Formatters.TryGetValue(relatedType, out var formatter) && formatter is ICustomJsonFormatter)
                {
                    return true;
                }

                if (Nullable.GetUnderlyingType(relatedType) != null)
                {
                    return HasCustomFormatterForRelatedType(relatedType); // we need to recurse if the related type is again nullable
                }
            }

            return false;
        }

        private static IJsonFormatter GetIntegrated(Type type)
        {
            var allTypes = typeof(IntegratedFormatterBuilder).Assembly.GetTypes();
            foreach (var candidate in allTypes.Where(a => a.IsPublic))
            {
                if (candidate.TryGetTypeOfGenericInterface(typeof(ICustomJsonFormatter<>), out _))
                {
                    continue;
                }

                if (candidate.TryGetTypeOfGenericInterface(typeof(IJsonFormatter<,>), out var argumentTypes) && argumentTypes.Length == 2)
                {
                    if (argumentTypes[0] == type && argumentTypes[1] == typeof(TSymbol))
                    {
                        // if it has a custom formatter for a base type (i.e. nullable base type, array element, list element)
                        // we need to ignore the integrated types for this
                        if (HasCustomFormatterForRelatedType(type))
                        {
                            continue;
                        }

                        return GetDefaultOrCreate(candidate);
                    }
                }
            }

            return null;
        }

        public IJsonFormatter BuildFormatter(Type type)
        {
            var integrated = GetIntegrated(type);
            if (integrated != null)
            {
                return integrated;
            }

            if (type == typeof(object))
            {
                return GetDefaultOrCreate(typeof(RuntimeFormatter<TSymbol, TResolver>));
            }

            if (type.IsArray)
            {
                var rank = type.GetArrayRank();
                switch (rank)
                {
                    case 1:
                        return GetDefaultOrCreate(typeof(ArrayFormatter<,,>).MakeGenericType(type.GetElementType(),
                            typeof(TSymbol), typeof(TResolver)));
                    case 2:
                        return GetDefaultOrCreate(typeof(TwoDimensionalArrayFormatter<,,>).MakeGenericType(type.GetElementType(),
                            typeof(TSymbol), typeof(TResolver)));
                    default:
                        throw new NotSupportedException("Only One- and Two-dimensional arrrays are supported.");
                }


            }

            if (type.IsEnum)
            {
                switch (_spanJsonOptions.EnumOption)
                {
                    case EnumOptions.String:
                    {
                        if (type.GetCustomAttribute<FlagsAttribute>() != null)
                        {
                            var enumBaseType = Enum.GetUnderlyingType(type);
                            return GetDefaultOrCreate(
                                typeof(EnumStringFlagsFormatter<,,,>).MakeGenericType(type, enumBaseType, typeof(TSymbol), typeof(TResolver)));
                        }

                        return GetDefaultOrCreate(typeof(EnumStringFormatter<,,>).MakeGenericType(type, typeof(TSymbol), typeof(TResolver)));
                    }
                    case EnumOptions.Integer:
                        return GetDefaultOrCreate(typeof(EnumIntegerFormatter<,,>).MakeGenericType(type, typeof(TSymbol), typeof(TResolver)));
                }
            }

            if (typeof(IDynamicMetaObjectProvider).IsAssignableFrom(type))
            {
                return GetDefaultOrCreate(typeof(DynamicMetaObjectProviderFormatter<,,>).MakeGenericType(type, typeof(TSymbol), typeof(TResolver)));
            }

            if (type.TryGetTypeOfGenericInterface(typeof(IDictionary<,>), out var dictArgumentTypes) && !IsBadDictionary(type))
            {
                if (dictArgumentTypes.Length != 2 || dictArgumentTypes[0] != typeof(string))
                {
                    throw new NotImplementedException($"{dictArgumentTypes[0]} is not supported a Key for Dictionary.");
                }

                return GetDefaultOrCreate(typeof(DictionaryFormatter<,,,>).MakeGenericType(type, dictArgumentTypes[1], typeof(TSymbol), typeof(TResolver)));
            }

            if (type.TryGetTypeOfGenericInterface(typeof(IReadOnlyDictionary<,>), out var rodictArgumentTypes))
            {
                if (rodictArgumentTypes.Length != 2 || rodictArgumentTypes[0] != typeof(string))
                {
                    throw new NotImplementedException($"{rodictArgumentTypes[0]} is not supported a Key for Dictionary.");
                }

                return GetDefaultOrCreate(
                    typeof(ReadOnlyDictionaryFormatter<,,,>).MakeGenericType(type, rodictArgumentTypes[1], typeof(TSymbol), typeof(TResolver)));
            }

            if (type.TryGetTypeOfGenericInterface(typeof(IList<>), out var listArgumentTypes) && !IsBadList(type))
            {
                return GetDefaultOrCreate(typeof(ListFormatter<,,,>).MakeGenericType(type, listArgumentTypes.Single(), typeof(TSymbol), typeof(TResolver)));
            }

            if (type.TryGetTypeOfGenericInterface(typeof(IEnumerable<>), out var enumArgumentTypes))
            {
                return GetDefaultOrCreate(
                    typeof(EnumerableFormatter<,,,>).MakeGenericType(type, enumArgumentTypes.Single(), typeof(TSymbol), typeof(TResolver)));
            }

            if (type.TryGetNullableUnderlyingType(out var underlyingType))
            {
                return GetDefaultOrCreate(typeof(NullableFormatter<,,>).MakeGenericType(underlyingType,
                    typeof(TSymbol), typeof(TResolver)));
            }

            // no integrated type, let's build it
            if (type.IsValueType)
            {
                return GetDefaultOrCreate(
                    typeof(ComplexStructFormatter<,,>).MakeGenericType(type, typeof(TSymbol), typeof(TResolver)));
            }

            return GetDefaultOrCreate(typeof(ComplexClassFormatter<,,>).MakeGenericType(type, typeof(TSymbol), typeof(TResolver)));
        }

        protected virtual bool IsBadDictionary(Type type)
        {
            // ReadOnlyDictionary is kinda broken, it implements IDictionary<T> too, but without any standard ctor
            // Make sure this is using the ReadOnlyDictionaryFormatter
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ReadOnlyDictionary<,>))
            {
                return true;
            }

            return false;
        }

        protected virtual bool IsBadList(Type type)
        {
            // ReadOnlyCollection is kinda broken, it implements IList<T> too, but without any standard ctor
            // Make sure this is using the EnumerableFormatter
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ReadOnlyCollection<>))
            {
                return true;
            }

            return false;
        }
    }
}
