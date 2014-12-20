using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Metadata;

namespace WebApiExtensions.Services
{
    public class MediaTypeParameterBinding : HttpParameterBinding
    {
        readonly bool _willReadBody;
        public override bool WillReadBody
        {
            get { return _willReadBody; }
        }

        public MediaTypeParameterBinding(HttpParameterDescriptor parameter, bool willReadBody)
            : base(parameter)
        {
            _willReadBody = willReadBody;
        }

        public override async Task ExecuteBindingAsync(ModelMetadataProvider metadataProvider, HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            if (_willReadBody)
                await actionContext.Request.ReadFormDataAsync(this.Descriptor.Configuration, cancellationToken);

            var paramName = this.Descriptor.ParameterName;
            var name = paramName.EndsWith("MediaType")
                ? paramName.Substring(0, paramName.Length - 9)
                : paramName;
            var model = actionContext.Request.GetMultiPartMediaType(name);

            SetValue(actionContext, model);
        }
    }
}
