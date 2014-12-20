using System.Collections.Generic;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using Newtonsoft.Json;
using WebApiExtensions.Formatters;
using WebApiExtensions.Handlers;
using WebApiExtensions.Services;

// ReSharper disable once CheckNamespace
namespace System.Web.Http
{
    public static class HttpConfigurationExtensions
    {
        public static void SupportJsonp(this HttpConfiguration config)
        {
            var oldFormatter = config.Formatters.JsonFormatter;
            if (oldFormatter != null)
            {
                JsonpFormatter.Default.SerializerSettings = oldFormatter.SerializerSettings;
                config.Formatters[config.Formatters.IndexOf(oldFormatter)] = JsonpFormatter.Default;
            }
            else
                config.Formatters.Add(JsonpFormatter.Default);

            config.MessageHandlers.Add(new JsonpHandler());
        }

        public static void ExtendXmlFormatter(this HttpConfiguration config, JsonSerializerSettings serializerSettings = null)
        {
            var oldFormatter = config.Formatters.XmlFormatter;
            var extendedXmlFormatter = new ExtendedXmlFormatter(serializerSettings);
            if (oldFormatter != null)
                config.Formatters[config.Formatters.IndexOf(oldFormatter)] = extendedXmlFormatter;
            else
                config.Formatters.Add(extendedXmlFormatter);
        }

        public static void SupportGraphController(this HttpConfiguration config)
        {
            var controllerSelector = new ApiControllerSelector(config);
            config.Services.Replace(typeof(IHttpControllerSelector), controllerSelector);
            config.Services.Replace(typeof(IHttpActionSelector), controllerSelector);
            config.Routes.MapHttpRoute("default", "{*path}", null,
                new
                {
                    controller = controllerSelector
                });
        }

        public static void ExtendModelBinding(this HttpConfiguration config)
        {
            config.Services.Replace(typeof(IActionValueBinder), new ApiActionValueBinder());
        }

        public static void SupportActionInjection(this HttpConfiguration config)
        {
            config.ParameterBindingRules.Add(parameter =>
            {
                if (!parameter.ParameterType.IsInterface)
                    return null;
                if (!parameter.ParameterType.IsGenericType || parameter.ParameterType.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                    return new InjectionParameterBinding(parameter, false);
                var type = parameter.ParameterType.GetGenericArguments()[0];
                return !type.IsInterface ? null : new InjectionParameterBinding(parameter, true);
            });
        }

        internal static JsonSerializer GetJsonSerializer(this HttpConfiguration config)
        {
            var serializer = (JsonSerializer)config.Properties.GetValueOrDefault("AZ_JsonSerializer");
            if (serializer != null)
                return serializer;

            serializer = JsonSerializer.Create(config.Formatters.JsonFormatter.SerializerSettings);
            config.Properties["AZ_JsonSerializer"] = serializer;
            return serializer;
        }

    }
}
