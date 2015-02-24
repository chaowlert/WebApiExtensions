using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace WebApiExtensions.Services
{
    public class InjectAttribute : ParameterBindingAttribute
    {
        public override HttpParameterBinding GetBinding(HttpParameterDescriptor parameter)
        {
            if (!parameter.ParameterType.IsGenericType || parameter.ParameterType.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                return new InjectionParameterBinding(parameter, false);
            else
                return new InjectionParameterBinding(parameter, true);
        }
    }
}
