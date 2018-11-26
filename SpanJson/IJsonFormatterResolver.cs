using System;
using System.Dynamic;
using SpanJson.Resolvers;

namespace SpanJson
{
    public interface IJsonFormatterResolver<TSymbol, in TResolver>
        where TResolver : IJsonFormatterResolver<TSymbol, TResolver>, new() where TSymbol : struct
    {
        IJsonFormatter GetFormatter(Type type);
        IJsonFormatter GetFormatter(JsonMemberInfo info, Type overrideMemberType = null);
        IJsonFormatter<T, TSymbol> GetFormatter<T>();
        JsonObjectDescription GetObjectDescription<T>();
        JsonObjectDescription GetDynamicObjectDescription(IDynamicMetaObjectProvider provider);

        Func<T> GetCreateFunctor<T>();
        Func<T, TConverted> GetEnumerableConvertFunctor<T, TConverted>();
    }

    public interface IJsonFormatterBuilder<TSymbol, in TResolver> where TResolver : IJsonFormatterResolver<TSymbol, TResolver>, new() where TSymbol : struct
    {
        IJsonFormatter BuildFormatter(Type type);
    }
}