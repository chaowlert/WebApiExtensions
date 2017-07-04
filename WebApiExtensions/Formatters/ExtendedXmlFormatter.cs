using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using JsonNet.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebApiExtensions.Formatters
{
    public class ExtendedXmlFormatter : XmlMediaTypeFormatter
    {
        public override bool CanReadType(Type type)
        {
            return true;
        }

        public override bool CanWriteType(Type type)
        {
            return true;
        }

        readonly JsonSerializer _serializer;
        public ExtendedXmlFormatter(JsonSerializerSettings serializerSettings)
        {
            _serializer = JsonSerializer.Create(serializerSettings);
        }

        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger, CancellationToken cancellationToken)
        {
            var xdoc = XDocument.Load(readStream);
            return Task.FromResult(xdoc.ToJson().ToObject(type));
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext, CancellationToken cancellationToken)
        {
            var json = JToken.FromObject(value, _serializer);
            var xdoc = json.ToXml();
            xdoc.Save(writeStream);
            return TaskEx.CompletedTask;
        }
    }
}
