using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WebApiExtensions.Formatters;

namespace WebApiExtensions.Handlers
{
    public class JsonpHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            //resolve method
            var method = request.GetQueryParameter("method");
            if (method != null)
            {
                method = method.ToUpper();
                request.Method = new HttpMethod(method);
            }

            var response = await base.SendAsync(request, cancellationToken);

            //always success
            var callback = request.GetQueryParameter("callback");
            if (callback != null)
            {
                if (response.Content == null)
                {
                    if ((int)response.StatusCode >= 400)
                    {
                        response = request.CreateErrorResponse(response.StatusCode, (string)null);
                        response.StatusCode = HttpStatusCode.OK;
                    }
                    else
                        response = request.CreateResponse(HttpStatusCode.OK, (string)null);
                }
                else
                {
                    var objContent = response.Content as ObjectContent;
                    if (objContent?.Formatter is JsonpFormatter)
                        response.StatusCode = HttpStatusCode.OK;
                }
            }

            return response;
        }
    }
}
