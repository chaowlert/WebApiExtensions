using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Description;
using System.Web.Http.Dispatcher;
using System.Web.Routing;
using WebApiExtensions.Formatters;

namespace WebApiExtensions.Services
{
    public class ApiControllerSelector : IHttpControllerSelector, IHttpActionSelector, IRouteConstraint, IApiExplorer
    {
        readonly Dictionary<string, ApiActionMapper> _noAreaStore;
        readonly Dictionary<string, Dictionary<string, ApiActionMapper>> _areaStores;
        readonly HttpConfiguration _config;
        public ApiControllerSelector(HttpConfiguration config)
        {
            _config = config;
            var assembliesResolver = config.Services.GetAssembliesResolver();
            var stores = (from type in config.Services.GetHttpControllerTypeResolver().GetControllerTypes(assembliesResolver)
                          where type.Name.EndsWith("Controller") &&
                                type.Namespace.Contains(".Controllers")
                          select new
                          {
                              Name = type.Name.Substring(0, type.Name.Length - 10).ToLower(),
                              Area = type.Namespace.Split(new[] { ".Controllers" }, StringSplitOptions.None).Last().ToLower(),
                              Type = type,
                          } into item
                          group item by item.Area).ToDictionary(g => g.Key,
                                                                g => g.ToDictionary(item => item.Name,
                                                                                    item => new ApiActionMapper(config, (g.Key == string.Empty ? string.Empty : g.Key + '/') + item.Name, item.Type)));
            _noAreaStore = stores.GetValueOrDefault(string.Empty);
            if (_noAreaStore != null)
                stores.Remove(string.Empty);
            if (stores.Count > 0)
                _areaStores = stores;
        }

        public bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values, RouteDirection routeDirection)
        {
            var path = (string)values["path"];
            if (path == null)
                return false;
            var segments = path.Split(new[]
            {
                '/'
            }, StringSplitOptions.RemoveEmptyEntries);

            var i = 0;
            Dictionary<string, ApiActionMapper> store;
            if (_areaStores != null && segments.Length > 1 && _areaStores.TryGetValue(segments[0], out store))
                i = 1;
            else if (_noAreaStore != null)
                store = _noAreaStore;
            else
                return false;

            return processController(values, segments, i, store);
        }

        static bool processController(RouteValueDictionary values, string[] segments, int i, Dictionary<string, ApiActionMapper> store)
        {
            var controller = segments[i];
            ApiActionMapper selector;
            if (!store.TryGetValue(controller, out selector))
                return false;

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
            return (HttpControllerDescriptor)request.GetRouteData().Values["controllerDescriptor"];
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
        public Collection<ApiDescription> ApiDescriptions
        {
            get { return _apiDescriptions ?? (_apiDescriptions = generateApiDescriptions()); }
        }

        Collection<ApiDescription> generateApiDescriptions()
        {
            var document = _config.Services.GetDocumentationProvider();
            var list = new Collection<ApiDescription>();
            var type = typeof(ApiDescription);
            var parameterDescriptions = type.GetProperty("ParameterDescriptions");
            var supportedResponseFormatters = type.GetProperty("SupportedResponseFormatters");
            var responseDescriptions = type.GetProperty("ResponseDescription");
            var responseFormatters = new Collection<MediaTypeFormatter> { JsonpFormatter.Default };
            foreach (var controllerKvp in GetControllerMapping().OrderBy(it => it.Key))
                foreach (var actionLookup in GetActionMapping(controllerKvp.Value).OrderBy(it => it.Key))
                    foreach (var actionDesc in actionLookup)
                        foreach (var method in actionDesc.SupportedHttpMethods)
                        {
                            var desc = new ApiDescription
                            {
                                ActionDescriptor = actionDesc,
                                Documentation = document.GetDocumentation(actionDesc),
                                HttpMethod = method,
                                RelativePath = getRelativePath(controllerKvp.Key, actionDesc),
                                Route = null,
                            };
                            parameterDescriptions.SetValue(desc, getApiParameterDescription(actionDesc), null);
                            supportedResponseFormatters.SetValue(desc, responseFormatters, null);
                            responseDescriptions.SetValue(desc, getResponseDescription(actionDesc), null);
                            list.Add(desc);
                        }
            return list;
        }

        ResponseDescription getResponseDescription(HttpActionDescriptor actionDescriptor)
        {
            var doc = _config.Services.GetDocumentationProvider();
            return new ResponseDescription
            {
                DeclaredType = actionDescriptor.ControllerDescriptor.ControllerType,
                Documentation = doc.GetResponseDocumentation(actionDescriptor),
                ResponseType = actionDescriptor.ReturnType,
            };
        }

        Collection<ApiParameterDescription> getApiParameterDescription(HttpActionDescriptor actionDescriptor)
        {
            var doc = _config.Services.GetDocumentationProvider();
            var list = new Collection<ApiParameterDescription>();
            foreach (var param in actionDescriptor.GetParameters())
            {
                if (param.ParameterName == "ignore")
                    continue;
                if (_config.ParameterBindingRules.LookupBinding(param) != null)
                    continue;
                var desc = new ApiParameterDescription
                {
                    Name = param.ParameterName,
                    ParameterDescriptor = param,
                    Documentation = doc.GetDocumentation(param),
                    Source = ApiParameterSource.FromUri,
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
