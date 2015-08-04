using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

namespace WebApiExtensions.Formatters
{
    public class JsonpFormatter : JsonMediaTypeFormatter
    {
        string _callbackQueryParameter;

        public static JsonpFormatter Default { get; } = new JsonpFormatter();

        public bool IsJsonp { get; set; }

        public string CallbackQueryParameter
        {
            get { return _callbackQueryParameter ?? "callback"; }
            set { _callbackQueryParameter = value; }
        }

        static JsonpFormatter create()
        {
            var jsonFormatter = new JsonpFormatter
            {
                SerializerSettings = Default.SerializerSettings
            };
            //jsonFormatter.AddQueryStringMapping("format", "json", "application/json");
            return jsonFormatter;
        }

        public override MediaTypeFormatter GetPerRequestFormatterInstance(Type type, HttpRequestMessage request, MediaTypeHeaderValue mediaType)
        {
            string callback;
            if (isJsonpRequest(request, out callback))
                return create(callback);
            else
                return this;
        }

        static JsonpFormatter create(string callback)
        {
            var instance = create();
            instance.IsJsonp = true;
            instance.CallbackQueryParameter = callback;
            return instance;
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream stream, HttpContent content, TransportContext transportContext)
        {
            if (IsJsonp)
                return writeJsonpToStreamAsync(type, value, stream, content, transportContext);
            else
                return base.WriteToStreamAsync(type, value, stream, content, transportContext);
        }

        async Task writeJsonpToStreamAsync(Type type, object value, Stream stream, HttpContent content, TransportContext transportContext)
        {
            var error = value as HttpError;
            error?.Add("Error", true);

            var encoding = SelectCharacterEncoding(content?.Headers);
            using (var writer = new StreamWriter(stream, encoding, 4096, true))
            {
                writer.Write("/**/ " + CallbackQueryParameter + "(");
                writer.Flush();

                await base.WriteToStreamAsync(type, value, stream, content, transportContext);

                writer.Write(");");
                writer.Flush();
            }
        }

        bool isJsonpRequest(HttpRequestMessage request, out string callback)
        {
            //if (request.Method != HttpMethod.Get)
            //{
            //    callback = null;
            //    return false;
            //}

            callback = request.GetQueryParameter(CallbackQueryParameter);
            return !string.IsNullOrEmpty(callback);
        }
    }
}
