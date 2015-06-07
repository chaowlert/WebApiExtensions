using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.ModelBinding;
using Newtonsoft.Json.Linq;

namespace WebApiExtensions.Services
{
    public class ApiActionValueBinder : IActionValueBinder
    {
        static readonly HashSet<string> _noBodyActions = new HashSet<string>
        {
            "GET", "HEAD", "TRACE", "DELETE", "CONNECT", "MKCOL", "COPY", "MOVE", "UNLOCK", "OPTIONS",
        };

        static readonly Action<HttpActionBinding> _ensureOneBodyParameter =
            typeof(DefaultActionValueBinder).GetMethod("EnsureOneBodyParameter", BindingFlags.Static | BindingFlags.NonPublic)
                                            .CreateDelegate<Action<HttpActionBinding>>();
        public HttpActionBinding GetBinding(HttpActionDescriptor actionDescriptor)
        {
            var binders = actionDescriptor.GetParameters().Select(GetParameterBinding).ToArray();
            var actionBinding = new HttpActionBinding(actionDescriptor, binders);
            _ensureOneBodyParameter(actionBinding);
            return actionBinding;
        }

        static readonly MethodInfo _getTypeCode =
            typeof(JToken).Assembly.GetType("Newtonsoft.Json.Utilities.ConvertUtils")
                          .GetMethod("GetTypeCode", new[] {typeof(Type)});
        static HttpParameterBinding GetParameterBinding(HttpParameterDescriptor parameter)
        {
            var attr = parameter.ParameterBinderAttribute;
            bool fromBody = false;
            if (attr != null)
            {
                fromBody = attr is FromBodyAttribute;
                if (!fromBody)
                    return attr.GetBinding(parameter);
            }

            var bindingRules = parameter.Configuration.ParameterBindingRules;
            var binding = bindingRules.LookupBinding(parameter);
            if (binding != null)
            {
                return binding;
            }

            //if (TypeDescriptor.GetConverter(parameter.ParameterType).CanConvertFrom(typeof(string)))
            //    return new FromUriAttribute().GetBinding(parameter);

            bool willReadBody = false;
            if (parameter.ActionDescriptor.Properties.GetValueOrDefault("AZ_ValueBinder") == null)
            {
                willReadBody = parameter.ActionDescriptor.SupportedHttpMethods.Any(method => !_noBodyActions.Contains(method.Method));
                parameter.ActionDescriptor.Properties["AZ_ValueBinder"] = string.Empty; //anything not null
            }

            if (parameter.ParameterType == typeof(HttpContent))
                return new MultiPartHttpContentParameterBinding(parameter);

            var isObject = _getTypeCode.FastInvoke(null, parameter.ParameterType).ToString() == "Object";
            var validator = isObject ? parameter.Configuration.Services.GetBodyModelValidator() : null;
            return new ApiParameterBinding(parameter, willReadBody, validator, isObject, fromBody);
        }

    }
}
