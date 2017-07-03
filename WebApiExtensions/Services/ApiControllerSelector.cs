using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Description;
using System.Web.Http.Dispatcher;

namespace WebApiExtensions.Services
{
    public class ApiControllerSelector : IHttpControllerSelector, IHttpActionSelector, IApiExplorer
    {
        readonly Dictionary<string, ApiActionMapper> _noAreaStore;
        readonly Dictionary<string, Dictionary<string, ApiActionMapper>> _areaStores;
        readonly HttpConfiguration _config;
        public ApiControllerSelector(HttpConfiguration config, Func<string, string> nameConverter)
        {
            _config = config;
            var assembliesResolver = config.Services.GetAssembliesResolver();
            var stores = (from type in config.Services.GetHttpControllerTypeResolver().GetControllerTypes(assembliesResolver)
                          where type.Name.EndsWith("Controller") &&
                                type.Namespace.Contains(".Controllers")
                          select new
                          {
                              Name = nameConverter(type.Name.Substring(0, type.Name.Length - "Controller".Length)),
                              Area = (type.Namespace + ".").Split(new[] {".Controllers."}, StringSplitOptions.None).Last().ToLower(),
                              Type = type,
                          }
                          into item
                          group item by item.Area.Trim('.')).ToDictionary(g => g.Key,
                              g => g.ToDictionary(
                                  item => item.Name,
                                  item => new ApiActionMapper(config, (g.Key == string.Empty ? string.Empty : g.Key + '/') + item.Name, item.Type, nameConverter)));
            _noAreaStore = stores.GetValueOrDefault(string.Empty);
            if (_noAreaStore != null)
                stores.Remove(string.Empty);
            if (stores.Count > 0)
                _areaStores = stores;
        }

        static HttpControllerDescriptor processController(IDictionary<string, object> values, string[] segments, int i, Dictionary<string, ApiActionMapper> store)
        {
            var controller = segments[i];
            if (!store.TryGetValue(controller, out var selector))
                return null;

            return selector.Process(values, segments, i + 1);
        }

        Dictionary<string, HttpControllerDescriptor> _controllerMapping;
        public IDictionary<string, HttpControllerDescriptor> GetControllerMapping()
        {
            if (_controllerMapping != null)
                return _controllerMapping;

            var dict = new Dictionary<string, HttpControllerDescriptor>();
            if (_noAreaStore != null)
            {
                foreach (var kvp in _noAreaStore)
                    dict.Add(kvp.Key, kvp.Value.ControllerDescriptor);
            }
            if (_areaStores != null)
            {
                foreach (var area in _areaStores)
                {
                    foreach (var kvp in area.Value)
                        dict.Add(area.Key + '/' + kvp.Key, kvp.Value.ControllerDescriptor);
                }
            }
            return _controllerMapping = dict;
        }

        public HttpControllerDescriptor SelectController(HttpRequestMessage request)
        {
            var values = request.GetRouteData().Values;
            var path = (string)values["path"];
            if (path == null)
                return null;
            var segments = path.Split(new[]
            {
                '/'
            }, StringSplitOptions.RemoveEmptyEntries);

            var i = 0;
            if (_areaStores != null && segments.Length > 1 && _areaStores.TryGetValue(segments[0], out var store))
                i = 1;
            else if (_noAreaStore != null)
                store = _noAreaStore;
            else
                return null;

            return processController(values, segments, i, store);
        }

        public ILookup<string, HttpActionDescriptor> GetActionMapping(HttpControllerDescriptor controllerDescriptor)
        {
            var split = controllerDescriptor.ControllerName.Split('/');
            if (split.Length == 1)
                return _noAreaStore[split[0]].GetActionMapping();
            else
                return _areaStores[split[0]][split[1]].GetActionMapping();
        }

        public HttpActionDescriptor SelectAction(HttpControllerContext controllerContext)
        {
            var request = controllerContext.Request;
            var store = (Dictionary<string, HttpActionDescriptor>)request.GetRouteData().Values["actionStore"];
            var actionDescriptor = store.GetValueOrDefault(request.Method.Method);
            if (actionDescriptor == null)
                throw new HttpResponseException(HttpStatusCode.MethodNotAllowed);
            return actionDescriptor;
        }

        Collection<ApiDescription> _apiDescriptions;
        public Collection<ApiDescription> ApiDescriptions => _apiDescriptions ?? (_apiDescriptions = generateApiDescriptions());

        Collection<ApiDescription> generateApiDescriptions()
        {
            var document = _config.Services.GetDocumentationProvider();
            var list = new Collection<ApiDescription>();
            var type = typeof(ApiDescription);
            var parameterDescriptions = type.GetProperty(nameof(ApiDescription.ParameterDescriptions));
            var supportedResponseFormatters = type.GetProperty(nameof(ApiDescription.SupportedResponseFormatters));
            var supportedRequestBodyFormatters = type.GetProperty(nameof(ApiDescription.SupportedRequestBodyFormatters));
            var responseDescriptions = type.GetProperty(nameof(ApiDescription.ResponseDescription));
            foreach (var controllerKvp in GetControllerMapping()
                .Where(it => !it.Value.GetCustomAttributes<ApiExplorerSettingsAttribute>().Any(attr => attr.IgnoreApi))
                .OrderBy(it => it.Key))
            foreach (var actionLookup in GetActionMapping(controllerKvp.Value).OrderBy(it => actionTransform(it.Key)))
            foreach (var actionDesc in actionLookup
                .Where(it => !it.GetCustomAttributes<ApiExplorerSettingsAttribute>().Any(attr => attr.IgnoreApi)))
            foreach (var method in actionDesc.SupportedHttpMethods)
            {
                var desc = new ApiDescription
                {
                    ActionDescriptor = actionDesc,
                    Documentation = document?.GetDocumentation(actionDesc),
                    HttpMethod = method,
                    RelativePath = getRelativePath(controllerKvp.Key, actionDesc),
                    Route = null,
                };
                parameterDescriptions.SetValue(desc, getApiParameterDescription(actionDesc), null);
                supportedResponseFormatters.SetValue(desc, getResponseFormatters(actionDesc), null);
                supportedRequestBodyFormatters.SetValue(desc, getRequestBodyFormatters(actionDesc), null);
                responseDescriptions.SetValue(desc, getResponseDescription(actionDesc), null);
                list.Add(desc);
            }
            return list;
        }

        static string actionTransform(string action)
        {
            switch (action)
            {
                case "items": return "  items";
                case "item": return " item";
                default: return action;
            }
        }

        Collection<MediaTypeFormatter> getResponseFormatters(HttpActionDescriptor actionDescriptor)
        {
            var responseFormatters = new Collection<MediaTypeFormatter>();
            foreach (var formatter in _config.Formatters)
            {
                if (actionDescriptor.ReturnType == null ||
                    !formatter.CanWriteType(actionDescriptor.ReturnType))
                    continue;
                responseFormatters.Add(formatter);
            }
            return responseFormatters;
        }
        Collection<MediaTypeFormatter> getRequestBodyFormatters(HttpActionDescriptor actionDescriptor)
        {
            var requestBodyFormatters = new Collection<MediaTypeFormatter>();
            foreach (var formatter in _config.Formatters)
            {
                if (actionDescriptor.ReturnType == null ||
                    !formatter.CanReadType(actionDescriptor.ReturnType))
                    continue;
                requestBodyFormatters.Add(formatter);
            }
            return requestBodyFormatters;
        }
        ResponseDescription getResponseDescription(HttpActionDescriptor actionDescriptor)
        {
            var doc = _config.Services.GetDocumentationProvider();
            return new ResponseDescription
            {
                DeclaredType = actionDescriptor.ControllerDescriptor.ControllerType,
                Documentation = doc?.GetResponseDocumentation(actionDescriptor),
                ResponseType = actionDescriptor.ReturnType,
            };
        }

        Collection<ApiParameterDescription> getApiParameterDescription(HttpActionDescriptor actionDescriptor)
        {
            var doc = _config.Services.GetDocumentationProvider();
            var list = new Collection<ApiParameterDescription>();
            var hasInjectionBinding = actionDescriptor.Configuration.ParameterBindingRules.Contains(HttpConfigurationExtensions.GetInjectionBinding);
            foreach (var param in actionDescriptor.GetParameters())
            {
                if (hasInjectionBinding)
                {
                    if (param.ParameterType.IsInterface)
                    {
                        var elementType = param.ParameterType.GetEnumerableElementType();
                        if (elementType == null || elementType.IsInterface)
                            continue;
                    }
                    if (param.ParameterBinderAttribute?.GetType() == typeof(InjectAttribute))
                        continue;
                }
                var desc = new ApiParameterDescription
                {
                    Name = param.ParameterName,
                    ParameterDescriptor = param,
                    Documentation = doc?.GetDocumentation(param),
                    Source = param.ParameterBinderAttribute?.GetType() == typeof(FromBodyAttribute)
                        ? ApiParameterSource.FromBody
                        : ApiParameterSource.FromUri,
                };
                list.Add(desc);
            }
            return list;
        }

        string getRelativePath(string controller, HttpActionDescriptor actionDescriptor)
        {
            if (actionDescriptor.ActionName == "items")
                return PathPrefix + "/" + controller;
            else if (actionDescriptor.ActionName == "item")
                return PathPrefix + "/" + controller + "/{id}";
            else if (actionDescriptor.GetParameters().Any(it => it.ParameterName == "id"))
                return PathPrefix + "/" + controller + "/{id}/" + actionDescriptor.ActionName;
            else
                return PathPrefix + "/" + controller + "/" + actionDescriptor.ActionName;
        }

        public string PathPrefix { get; set; }
    }
}
