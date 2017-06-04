using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;

namespace WebApiExtensions.Filters
{
    public class NullToNotFoundAttribute : ActionFilterAttribute
    {
        public string RouteParam { get; set; }

        public NullToNotFoundAttribute(): this("id") { }
        public NullToNotFoundAttribute(string routeParam)
        {
            this.RouteParam = routeParam;
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext.Exception != null)
                return;

            var objectContent = actionExecutedContext.Response.Content as ObjectContent;
            if (objectContent == null || objectContent.Value != null)
                return;

            var type = actionExecutedContext.ActionContext.ControllerContext.ControllerDescriptor.ControllerName;
            var id = actionExecutedContext.Request.GetRouteData(this.RouteParam);
            actionExecutedContext.Response = actionExecutedContext.Request.CreateNotFoundResponse(type, id);
        }
    }
}
