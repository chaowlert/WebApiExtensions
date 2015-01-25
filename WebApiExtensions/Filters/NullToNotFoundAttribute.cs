using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;

namespace WebApiExtensions.Filters
{
    public class NullToNotFoundAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext.Exception != null)
                return;

            var objectContent = actionExecutedContext.Response.Content as ObjectContent;
            if (objectContent == null || objectContent.Value != null)
                return;

            actionExecutedContext.Response = new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }
}
