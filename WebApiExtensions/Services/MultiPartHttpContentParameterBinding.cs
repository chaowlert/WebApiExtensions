using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Metadata;

namespace WebApiExtensions.Services
{
    public class MultiPartHttpContentParameterBinding : HttpParameterBinding
    {
        public MultiPartHttpContentParameterBinding(HttpParameterDescriptor parameter)
            : base(parameter) { }

        public override async Task ExecuteBindingAsync(ModelMetadataProvider metadataProvider, HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            await actionContext.Request.ReadFormDataAsync(this.Descriptor.Configuration, cancellationToken);

            var model = (object)actionContext.Request.GetMultiPartHttpContent(this.Descriptor.ParameterName);
            if (model == null)
            {
                if (this.Descriptor.IsOptional)
                    model = this.Descriptor.DefaultValue;
                else
                    throw new HttpResponseException(actionContext.Request.CreateRequiredResponse(this.Descriptor.ParameterName));
            }

            SetValue(actionContext, model);
        }

    }
}
