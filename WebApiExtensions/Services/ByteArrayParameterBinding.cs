using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Metadata;

namespace WebApiExtensions.Services
{
    public class ByteArrayParameterBinding : HttpParameterBinding
    {
        readonly bool _willReadBody;
        public override bool WillReadBody
        {
            get { return _willReadBody; }
        }

        public ByteArrayParameterBinding(HttpParameterDescriptor parameter, bool willReadBody) : base(parameter)
        {
            _willReadBody = willReadBody;
        }

        public override async Task ExecuteBindingAsync(ModelMetadataProvider metadataProvider, HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            if (_willReadBody)
                await actionContext.Request.ReadFormDataAsync(this.Descriptor.Configuration, cancellationToken);

            var model = actionContext.Request.GetMultiPartBytes(this.Descriptor.ParameterName);

            SetValue(actionContext, model);
        }
    }
}
