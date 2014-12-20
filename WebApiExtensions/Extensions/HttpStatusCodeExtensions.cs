using Chaow.WebApi.Contents;
using ExtraConstraints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace System.Net
{
    public static class HttpStatusCodeExtensions
    {
        public static HttpResponseMessage ToMessage(this HttpStatusCode statusCode, params Tuple<string, string>[] arguments)
        {
            return new HttpResponseMessage(statusCode) { Content = new ErrorContent(statusCode.ToString(), arguments) };
        }
        public static HttpResponseMessage ToMessage<[EnumConstraint]T>(this HttpStatusCode statusCode, T @enum, params Tuple<string, string>[] arguments)
        {
            return new HttpResponseMessage(statusCode) { Content = new ErrorContent(@enum.ToString(), arguments) };
        }
        public static HttpResponseMessage ToMessage(this HttpStatusCode statusCode, string message, params Tuple<string, string>[] arguments)
        {
            return new HttpResponseMessage(statusCode) { Content = new ErrorContent(statusCode.ToString(), arguments) { Message = message } };
        }
        public static HttpResponseMessage ToMessage<[EnumConstraint]T>(this HttpStatusCode statusCode, T @enum, string message, params Tuple<string, string>[] arguments)
        {
            return new HttpResponseMessage(statusCode) { Content = new ErrorContent(@enum.ToString(), arguments) { Message = message } };
        }
        public static HttpResponseException ToException(this HttpStatusCode statusCode, params Tuple<string, string>[] arguments)
        {
            return new HttpResponseException(statusCode.ToMessage(arguments));
        }
        public static HttpResponseException ToException<[EnumConstraint]T>(this HttpStatusCode statusCode, T @enum, params Tuple<string, string>[] arguments)
        {
            return new HttpResponseException(statusCode.ToMessage(@enum, arguments));
        }
        public static HttpResponseException ToException(this HttpStatusCode statusCode, string message, params Tuple<string, string>[] arguments)
        {
            return new HttpResponseException(statusCode.ToMessage(message, arguments));
        }
        public static HttpResponseException ToException<[EnumConstraint]T>(this HttpStatusCode statusCode, T @enum, string message, params Tuple<string, string>[] arguments)
        {
            return new HttpResponseException(statusCode.ToMessage(@enum, message, arguments));
        }
    }
}
