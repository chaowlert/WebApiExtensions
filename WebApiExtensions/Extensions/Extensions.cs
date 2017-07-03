using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Dependencies;

public static class Extensions
{
    internal static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
    {
        return new HashSet<T>(source);
    }

    public static T GetService<T>(this IDependencyScope scope)
    {
        return (T)scope.GetService(typeof(T));
    }

    public static string ToQueryString(this NameValueCollection data)
    {
        var strs = data.Cast<string>()
            .Select(name => string.Concat(name, "=", Uri.EscapeDataString(data[name])));
        return string.Join("&", strs);
    }
    
    public static HttpResponseException ToException(this HttpResponseMessage response)
    {
        return new HttpResponseException(response);
    }
    enum WordType
    {
        Unknown,
        UpperCase,
        LowerCase,
        ProperCase,
    }
    internal static IEnumerable<string> BreakWords(this string s)
    {
        var len = s.Length;
        var pos = 0;
        var last = 0;
        var type = WordType.Unknown;

        while (pos < len)
        {
            var c = s[pos];
            if (c == '_')
            {
                if (last < pos)
                {
                    yield return s.Substring(last, pos - last);
                    last = pos;
                    type = WordType.Unknown;
                }
                last++;
            }
            else if (char.IsUpper(c))
            {
                if (type == WordType.Unknown)
                    type = WordType.UpperCase;
                else if (type != WordType.UpperCase && last < pos)
                {
                    yield return s.Substring(last, pos - last);
                    last = pos;
                    type = WordType.UpperCase;
                }
            }
            else  //lower
            {
                if (type == WordType.Unknown)
                    type = WordType.LowerCase;
                else if (type == WordType.UpperCase)
                {
                    if (last < pos - 1)
                    {
                        yield return s.Substring(last, pos - last - 1);
                        last = pos - 1;
                    }
                    type = WordType.ProperCase;
                }
            }
            pos++;
        }
        if (last < pos)
            yield return s.Substring(last, pos - last);
    }

    internal static string Join(this IEnumerable<string> values, string separator)
    {
        return string.Join(separator, values);
    }
}
