using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Dependencies;

public static class Extensions
{
    public static T GetService<T>(this IDependencyResolver resolver)
    {
        return (T)resolver.GetService(typeof(T));
    }
    
    public static string ToQueryString(this NameValueCollection data)
    {
        var strs = data.Cast<string>()
            .Select(name => string.Concat(name, "=", HttpUtility.UrlEncode(data[name])));
        return string.Join("&", strs);
    }
    
    public static HttpResponseException ToException(this HttpResponseMessage response)
    {
        return new HttpResponseException(response);
    }

}
