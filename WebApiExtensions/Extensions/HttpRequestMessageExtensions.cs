using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Web.Http;

// ReSharper disable once CheckNamespace
namespace System.Net.Http
{
    public static class HttpRequestMessageExtensions
    {
        public static JObject GetJsonFromQueryString(this HttpRequestMessage request)
        {
            var json = (JObject)request.Properties.GetValueOrDefault("AZ_Query");
            if (json != null)
                return json;

            if (!request.RequestUri.TryReadQueryAsJson(out json))
                throw new HttpResponseException(request.CreateBadFormatResponse("queryString"));
            request.Properties["AZ_Query"] = json;
            return json;
        }

        public static async Task ReadFormDataAsync(this HttpRequestMessage request, HttpConfiguration config, CancellationToken cancellationToken)
        {
            var json = (JObject)request.Properties.GetValueOrDefault("AZ_Form");
            if (json != null)
                return;

            if (request.Content == null)
                request.Properties["AZ_Form"] =  new JObject();

            else if (request.Content.IsMimeMultipartContent("form-data"))
            {
                var tuple = await GetDataFromMultiPartAsync(request, cancellationToken);
                request.Properties["AZ_Form"] = tuple.Item1;
                request.Properties["AZ_MultiPartHttpContent"] = tuple.Item2;
            }
            else
            {
                try
                {
                    json = await request.Content.ReadAsAsync<JObject>(config.Formatters, cancellationToken);
                    request.Properties["AZ_Form"] = json;
                }
                catch (Exception ex)
                {
                    throw new HttpResponseException(request.CreateErrorResponse(HttpStatusCode.BadRequest, ex));
                }
            }
        }

        public static string GetQueryParameter(this HttpRequestMessage request, string name)
        {
            var json = request.GetJsonFromQueryString();
            return (string)json[name];
        }

        public static string GetFormParameter(this HttpRequestMessage request, string name)
        {
            var json = (JObject)request.Properties.GetValueOrDefault("AZ_Form");
            if (json != null)
                return (string)json[name];
            else
                return null;
        }

        public static string GetParameter(this HttpRequestMessage request, string name)
        {
            return request.GetQueryParameter(name) ?? request.GetFormParameter(name);
        }

        public static HttpContent GetMultiPartHttpContent(this HttpRequestMessage request, string name)
        {
            var dict = (Dictionary<string, HttpContent>)request.Properties.GetValueOrDefault("AZ_MultiPartHttpContent");
            return dict?.GetValueOrDefault(name);
        }

        public static JToken GetJsonParameters(this HttpRequestMessage request)
        {
            return (JObject)request.Properties.GetValueOrDefault("AZ_Form");
        }

        public static JToken GetJsonParameter(this HttpRequestMessage request, string name)
        {
            var queryString = request.GetJsonFromQueryString();
            var json = queryString[name];
            if (json != null)
                return json;

            var formData = request.GetJsonParameters();
            return formData?[name];
        }

        public static string GetMessage(this HttpRequestMessage request)
        {
            var sb = new StringBuilder();
            sb.Append(request.Method.Method).Append(' ').AppendLine(request.RequestUri.PathAndQuery);
            sb.AppendLine(request.Headers.ToString());
            if (request.Content != null)
            {
                sb.AppendLine(request.Content.Headers.ToString());
                sb.AppendLine();
                var json = request.GetJsonParameters();
                if (json != null)
                    sb.AppendLine(json.ToString(Newtonsoft.Json.Formatting.None));
            }
            return sb.ToString();
        }

        static async Task<Tuple<JObject, Dictionary<string, HttpContent>>> GetDataFromMultiPartAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var multipart = await request.Content.ReadAsMultipartAsync(cancellationToken);
            var queries = new List<string>();
            var httpContentDict = new Dictionary<string, HttpContent>();
            foreach (var c in multipart.Contents)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var h = c.Headers;
                if (h.ContentType != null || h.ContentDisposition.FileName != null)
                {
                    var name = h.ContentDisposition.Name ?? h.ContentDisposition.FileName;
                    if (string.IsNullOrEmpty(name))
                        continue;
                    name = name.Trim('"');
                    var bytes = await c.ReadAsByteArrayAsync();
                    var content = new ByteArrayContent(bytes);
                    if (h.ContentType != null)
                        content.Headers.ContentType = h.ContentType;
                    content.Headers.ContentDisposition = h.ContentDisposition;
                    httpContentDict.Add(name, content);
                }
                else
                {
                    var name = h.ContentDisposition.Name;
                    if (string.IsNullOrEmpty(name))
                        continue;
                    name = name.Trim('"');
                    var value = await c.ReadAsStringAsync();
                    queries.Add(name + '=' + value);
                }
            }
            var queryPart = string.Join("&", queries);
            var uri = new Uri("http://localhost?" + queryPart);
            JObject json;
            if (!uri.TryReadQueryAsJson(out json))
                throw new HttpResponseException(request.CreateBadFormatResponse("formData"));
            return Tuple.Create(json, httpContentDict);
        }
        public static string GetRouteData(this HttpRequestMessage request, string key)
        {
            var routeData = request.GetRouteData();
            return (string)routeData.Values[key];
        }

        public static HttpResponseMessage CreateErrorCodeResponse(this HttpRequestMessage request, HttpStatusCode statusCode, string message, JToken additionalInfo = null)
        {
            var error = new HttpError(message)
            {
                {"code", statusCode}
            };
            if (additionalInfo != null)
                error.Add("additionalInfo", additionalInfo);
            return request.CreateErrorResponse(statusCode, error);
        }
        public static HttpResponseMessage CreateRequiredResponse(this HttpRequestMessage request, string paramName)
        {
            var additionalInfo = new JObject
            {
                { "paramName", paramName },
            };
            return request.CreateErrorCodeResponse(HttpStatusCode.BadRequest, "Parameter is required", additionalInfo);
        }
        public static HttpResponseMessage CreateBadFormatResponse(this HttpRequestMessage request, string paramName, string format = null)
        {
            var additionalInfo = new JObject
            {
                { "paramName", paramName },
            };
            if (format != null)
                additionalInfo["format"] = format;
            return request.CreateErrorCodeResponse(HttpStatusCode.BadRequest, "Input value is incorrect format", additionalInfo);
        }
        public static HttpResponseMessage CreateNotFoundResponse(this HttpRequestMessage request, string type, string id)
        {
            var additionalInfo = new JObject
            {
                { "type", type },
                { "id", id },
            };
            return request.CreateErrorCodeResponse(HttpStatusCode.NotFound, "Object is not found", additionalInfo);
        }
    }
}
