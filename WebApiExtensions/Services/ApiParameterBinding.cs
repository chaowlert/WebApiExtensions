using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Metadata;
using System.Web.Http.Validation;

namespace WebApiExtensions.Services
{
    public class ApiParameterBinding : HttpParameterBinding
    {
        readonly bool _willReadBody;
        public override bool WillReadBody
        {
            get { return _willReadBody; }
        }

        readonly IBodyModelValidator _validator;
        readonly bool _isObject;
        readonly bool _fromBody;
        public ApiParameterBinding(HttpParameterDescriptor descriptor, bool willReadBody, IBodyModelValidator validator, bool isObject, bool fromBody) : base(descriptor)
        {
            _willReadBody = willReadBody;
            _validator = validator;
            _isObject = isObject;
            _fromBody = fromBody;
        }

        public override async Task ExecuteBindingAsync(ModelMetadataProvider metadataProvider, HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            if (_willReadBody)
                await actionContext.Request.ReadFormDataAsync(this.Descriptor.Configuration, cancellationToken);

            var model = GetObjectValue(actionContext);
            SetValue(actionContext, model);

            if (_validator != null)
                _validator.Validate(model, this.Descriptor.ParameterType, metadataProvider, actionContext, this.Descriptor.ParameterName);
        }

        object GetObjectValue(HttpActionContext actionContext)
        {
            var json = _fromBody 
                ? actionContext.Request.GetJsonParameters()
                : actionContext.Request.GetJsonParameter(this.Descriptor.ParameterName);
            if (json == null)
            {
                if (this.Descriptor.IsOptional)
                    return this.Descriptor.DefaultValue;
                else
                    throw new HttpResponseException(actionContext.Request.CreateRequiredResponse(this.Descriptor.ParameterName));
            }

            try
            {
                if (_isObject)
                    return json.ToObject(this.Descriptor.ParameterType, this.Descriptor.Configuration.GetJsonSerializer());
                else
                    return json.ToObject(this.Descriptor.ParameterType);
            }
            catch (Exception ex)
            {
                throw new HttpResponseException(actionContext.Request.CreateErrorResponse(HttpStatusCode.UnsupportedMediaType, ex));
            }
        }
    }
}