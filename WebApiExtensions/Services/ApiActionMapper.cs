using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.ValueProviders;
using System.Web.Http.ValueProviders.Providers;

namespace WebApiExtensions.Services
{
    public class ApiActionMapper
    {
        public HttpControllerDescriptor ControllerDescriptor { get; }

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
            if (action.ActionName == "items")
            {
                return _itemsStore ?? (_itemsStore = new Dictionary<string, HttpActionDescriptor>());
            }
            else if (param.Take(1).Any(p => p.ParameterName == "id"))
            {
                //set route attribute for id
                var id = param[0];
                if (id.ParameterBinderAttribute == null)
                    id.ParameterBinderAttribute = new ValueProviderAttribute(typeof(RouteDataValueProviderFactory));

                if (action.ActionName == "item")
                {
                    return _itemStore ?? (_itemStore = new Dictionary<string, HttpActionDescriptor>());
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
                if (_nonItemActionStores == null)
                    _nonItemActionStores = new Dictionary<string, Dictionary<string, HttpActionDescriptor>>();
                return _nonItemActionStores.GetOrAdd(action.ActionName, key => new Dictionary<string, HttpActionDescriptor>());
            }            
        }

        public HttpControllerDescriptor Process(IDictionary<string, object> values, string[] segments, int i)
        {
            values.Add("controller", ControllerDescriptor.ControllerName);
            //values.Add("controllerDescriptor", ControllerDescriptor);

            if (segments.Length <= i)
            {
                if (_itemsStore == null)
                    return null;
                values.Add("action", "items");
                values.Add("actionStore", _itemsStore);
                return ControllerDescriptor;
            }

            var id = segments[i];
            i++;
            if (segments.Length > i)
            {
                if (_itemActionStores == null || segments.Length > i + 1)
                    return null;
                var action = segments[i];
                var itemActionStore = _itemActionStores.GetValueOrDefault(action);
                if (itemActionStore == null)
                    return null;
                values.Add("action", action);
                values.Add("id", id);
                values.Add("actionStore", itemActionStore);
                return ControllerDescriptor;
            }

            var nonItemActionStore = _nonItemActionStores?.GetValueOrDefault(id);
            if (nonItemActionStore != null)
            {
                values.Add("action", id);
                values.Add("actionStore", nonItemActionStore);
                return ControllerDescriptor;
            }
            if (_itemStore != null)
            {
                values.Add("action", "item");
                values.Add("id", id);
                values.Add("actionStore", _itemStore);
                return ControllerDescriptor;
            }
            return null;
        }

        ILookup<string, HttpActionDescriptor> _actionMapping;
        public ILookup<string, HttpActionDescriptor> GetActionMapping()
        {
            if (_actionMapping != null)
                return _actionMapping;

            var dict = new Dictionary<string, List<HttpActionDescriptor>>();
            if (_itemStore != null)
            {
                dict.Add("item", (from m in _itemStore
                                  orderby methodOrder(m.Key)
                                  select m.Value).ToList());
            }
            if (_itemsStore != null)
            {
                dict.Add("items", (from m in _itemsStore
                                   orderby methodOrder(m.Key)
                                   select m.Value).ToList());
            }
            if (_nonItemActionStores != null)
            {
                foreach (var kvp in _nonItemActionStores)
                {
                    dict.Add(kvp.Key, (from m in kvp.Value
                                       orderby methodOrder(m.Key)
                                       select m.Value).ToList());
                }
            }
            if (_itemActionStores != null)
            {
                foreach (var kvp in _itemActionStores)
                {
                    if (!dict.TryGetValue(kvp.Key, out List<HttpActionDescriptor> list))
                    {
                        list = new List<HttpActionDescriptor>();
                        dict.Add(kvp.Key, list);
                    }
                    list.AddRange(from m in kvp.Value
                                  orderby methodOrder(m.Key)
                                  select m.Value);
                }
            }
            return _actionMapping = (from kvp in dict
                                     from method in kvp.Value
                                     select new { kvp.Key, Value = method }).ToLookup(item => item.Key, item => item.Value);
        }
        static int methodOrder(string method)
        {
            switch (method)
            {
                case "GET": return 1;
                case "POST": return 2;
                case "PUT": return 3;
                case "DELETE": return 4;
                default: return 5;
            }
        }
    }
}
