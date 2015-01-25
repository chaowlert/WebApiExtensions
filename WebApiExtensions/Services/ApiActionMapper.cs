using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.ValueProviders;
using System.Web.Http.ValueProviders.Providers;
using System.Web.Routing;

namespace WebApiExtensions.Services
{
    public class ApiActionMapper
    {
        public HttpControllerDescriptor ControllerDescriptor { get; private set; }

        Dictionary<string, HttpActionDescriptor> _itemStore;
        Dictionary<string, HttpActionDescriptor> _itemsStore;
        Dictionary<string, Dictionary<string, HttpActionDescriptor>> _itemActionStores;
        Dictionary<string, Dictionary<string, HttpActionDescriptor>> _nonItemActionStores;
        public ApiActionMapper(HttpConfiguration config, string name, Type type)
        {
            ControllerDescriptor = new HttpControllerDescriptor(config, name, type);
            foreach (var methodInfo in ControllerDescriptor.ControllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                var attributes = methodInfo.GetCustomAttributes();
                if (!attributes.OfType<IActionHttpMethodProvider>().Any())
                    continue;

                var action = new ReflectedHttpActionDescriptor(ControllerDescriptor, methodInfo);
                var store = SelectStore(action);
                foreach (var httpMethod in action.SupportedHttpMethods)
                    store.Add(httpMethod.Method, action);
            }
        }

        Dictionary<string, HttpActionDescriptor> SelectStore(HttpActionDescriptor action)
        {
            var param = action.GetParameters();
            if (param.Take(1).Any(p => p.ParameterName == "id"))
            {
                //if (action.ActionName == "items")
                //    throw new InvalidOperationException("'items' action cannot contain 'id' parameter");

                //set route attribute for id
                var id = param[0];
                if (id.ParameterBinderAttribute == null)
                    id.ParameterBinderAttribute = new ValueProviderAttribute(typeof(RouteDataValueProviderFactory));

                if (action.ActionName == "item")
                {
                    if (_itemStore == null)
                        _itemStore = new Dictionary<string, HttpActionDescriptor>();
                    return _itemStore;
                }
                else
                {
                    if (_itemActionStores == null)
                        _itemActionStores = new Dictionary<string, Dictionary<string, HttpActionDescriptor>>();
                    return _itemActionStores.GetOrAdd(action.ActionName, key => new Dictionary<string, HttpActionDescriptor>());
                }
            }
            else
            {
                if (action.ActionName == "item")
                    throw new InvalidOperationException("'item' action must have 'id' parameter");
                if (action.ActionName == "items")
                {
                    if (_itemsStore == null)
                        _itemsStore = new Dictionary<string, HttpActionDescriptor>();
                    return _itemsStore;
                }
                else
                {
                    if (_nonItemActionStores == null)
                        _nonItemActionStores = new Dictionary<string, Dictionary<string, HttpActionDescriptor>>();
                    return _nonItemActionStores.GetOrAdd(action.ActionName, key => new Dictionary<string, HttpActionDescriptor>());
                }
            }            
        }

        public bool Process(RouteValueDictionary values, string[] segments, int i)
        {
            values.Add("controller", ControllerDescriptor.ControllerName);
            values.Add("controllerDescriptor", ControllerDescriptor);

            if (segments.Length <= i)
            {
                if (_itemsStore == null)
                    return false;
                values.Add("action", "items");
                values.Add("actionStore", _itemsStore);
                return true;
            }

            var id = segments[i];
            i++;
            if (segments.Length > i)
            {
                if (_itemActionStores == null || segments.Length > i + 1)
                    return false;
                var action = segments[i];
                var itemActionStore = _itemActionStores.GetValueOrDefault(action);
                if (itemActionStore == null)
                    return false;
                values.Add("action", action);
                values.Add("id", id);
                values.Add("actionStore", itemActionStore);
                return true;
            }

            if (_itemStore != null)
            {
                values.Add("action", "item");
                values.Add("id", id);
                values.Add("actionStore", _itemStore);
                return true;
            }
            if (_nonItemActionStores == null)
                return false;
            var nonItemActionStore = _nonItemActionStores.GetValueOrDefault(id);
            if (nonItemActionStore == null)
                return false;
            values.Add("action", id);
            values.Add("actionStore", nonItemActionStore);
            return true;
        }

        ILookup<string, HttpActionDescriptor> _actionMapping;
        public ILookup<string, HttpActionDescriptor> GetActionMapping()
        {
            if (_actionMapping != null)
                return _actionMapping;

            var dict = new Dictionary<string, Dictionary<string, HttpActionDescriptor>>();
            if (_itemStore != null)
                dict.Add("item", _itemStore);
            if (_itemsStore != null)
                dict.Add("items", _itemsStore);
            if (_itemActionStores != null)
            {
                foreach (var kvp in _itemActionStores)
                    dict.Add(kvp.Key, kvp.Value);
            }
            if (_nonItemActionStores != null)
            {
                foreach (var kvp in _nonItemActionStores)
                    dict.Add(kvp.Key, kvp.Value);
            }
            return _actionMapping = (from kvp in dict
                                     from method in kvp.Value
                                     select new { kvp.Key, method.Value }).ToLookup(item => item.Key, item => item.Value);
        }
    }
}
