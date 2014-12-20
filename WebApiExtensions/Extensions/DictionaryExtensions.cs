using System.Threading.Tasks;
// ReSharper disable once CheckNamespace


namespace System.Collections.Generic
{
    static class DictionaryExtensions
    {
        public static U GetValueOrDefault<T, U>(this IDictionary<T, U> dict, T key)
        {
            U value;
            return dict.TryGetValue(key, out value) ? value : default(U);
        }

        public static U GetOrAdd<T, U>(this IDictionary<T, U> dict, T key, Func<T, U> func)
        {
            U value;
            if (dict.TryGetValue(key, out value))
                return value;
            value = func(key);
            dict.Add(key, value);
            return value;
        }
    }
}
