using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Metadata;

namespace WebApiExtensions.Services
{
    public class InjectionParameterBinding : HttpParameterBinding
    {
        readonly bool _isEnumerable;
        readonly Type _type;
        public InjectionParameterBinding(HttpParameterDescriptor parameter, bool isEnumerable) : base(parameter)
        {
            _isEnumerable = isEnumerable;
            _type = isEnumerable ? parameter.ParameterType.GenericTypeArguments[0] : parameter.ParameterType;
        }

        public override Task ExecuteBindingAsync(ModelMetadataProvider metadataProvider, HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            var resolver = actionContext.RequestContext.Configuration.DependencyResolver;
            var model = _isEnumerable ? resolver.GetServices(_type) : resolver.GetService(_type);
            SetValue(actionContext, model);
            return TaskEx.CompletedTask;
        }
    }
}
