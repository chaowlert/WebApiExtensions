using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;

// ReSharper disable once CheckNamespace
namespace System.Reflection
{
    static class MethodInfoExtensions
    {
        public static TDelegate CreateDelegate<TDelegate>(this MethodInfo methodInfo)
        {
            return (TDelegate)(object)methodInfo.CreateDelegate(typeof(TDelegate));
        }
        public static TDelegate CreateDelegate<TDelegate>(this MethodInfo methodInfo, object obj)
        {
            return (TDelegate)(object)methodInfo.CreateDelegate(typeof(TDelegate), obj);
        }
        static readonly ConcurrentDictionary<MethodInfo, Func<object, object[], object>> _methodDict =
            new ConcurrentDictionary<MethodInfo, Func<object, object[], object>>();
        public static object FastInvoke(this MethodInfo methodInfo, object instance, params object[] args)
        {
            var func = _methodDict.GetOrAdd(methodInfo, CreateFastInvoke);
            return func(instance, args);
        }

        static Func<object, object[], object> CreateFastInvoke(MethodInfo methodInfo)
        {
            var p1 = Expression.Parameter(typeof(object));
            var p2 = Expression.Parameter(typeof(object[]));

            var args = methodInfo.GetParameters().Select((p, i) =>
                Expression.Convert(Expression.ArrayIndex(p2, Expression.Constant(i)), p.ParameterType));

            var call = methodInfo.IsStatic ?
                Expression.Call(methodInfo, args) :
                Expression.Call(Expression.Convert(p1, methodInfo.DeclaringType), methodInfo, args);

            var body = Expression.Convert(call, typeof(object));

            return Expression.Lambda<Func<object, object[], object>>(body, p1, p2).Compile();
        } 
    }
}
