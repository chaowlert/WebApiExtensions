using System.Collections.ObjectModel;
using System.Linq;
using System.Web.Http.Controllers;
using System.Web.Http.Description;

namespace WebApiExtensions.Services
{
    public class XmlDocumentationProviderCollection : Collection<IDocumentationProvider>, IDocumentationProvider
    {
        public string GetDocumentation(HttpParameterDescriptor parameterDescriptor)
        {
            return this.Select(d => d.GetDocumentation(parameterDescriptor)).FirstOrDefault(s => s != null);
        }

        public string GetDocumentation(HttpActionDescriptor actionDescriptor)
        {
            return this.Select(d => d.GetDocumentation(actionDescriptor)).FirstOrDefault(s => s != null);
        }

        public string GetDocumentation(HttpControllerDescriptor controllerDescriptor)
        {
            return this.Select(d => d.GetDocumentation(controllerDescriptor)).FirstOrDefault(s => s != null);
        }

        public string GetResponseDocumentation(HttpActionDescriptor actionDescriptor)
        {
            return this.Select(d => d.GetDocumentation(actionDescriptor)).FirstOrDefault(s => s != null);
        }
    }
}
