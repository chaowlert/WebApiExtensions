using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;

namespace WebApiExtensions.Filters
{
    public class ExceptionHandleAttribute : ExceptionFilterAttribute
    {
        public Type ExceptionType { get; set; }
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.Conflict;
        public string Message { get; set; }

        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext.Exception.GetType() == this.ExceptionType)
            {
                actionExecutedContext.Response = actionExecutedContext.Request.CreateErrorCodeResponse(this.StatusCode, this.Message);
            }
        }
    }
}
