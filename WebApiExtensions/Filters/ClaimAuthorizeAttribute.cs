using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace WebApiExtensions.Filters
{
    public class ClaimAuthorizeAttribute : AuthorizationFilterAttribute
    {
        public string ClaimType { get; set; }

        private string _claimValues;
        private HashSet<string> _claimValueSplit;
        public string ClaimValues
        {
            get => _claimValues;
            set
            {
                _claimValues = string.IsNullOrEmpty(value) ? null : value;
                _claimValueSplit = value?.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToHashSet();
            }
        }

        private string _roles;
        private List<string> _roleSplit;
        public string Roles
        {
            get => _roles;
            set
            {
                _roles = string.IsNullOrEmpty(value) ? null : value;
                _roleSplit = value?.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }
        }

        public override Task OnAuthorizationAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            var principal = actionContext.RequestContext.Principal as ClaimsPrincipal;

            if (principal == null || !principal.Identity.IsAuthenticated)
            {
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
                return TaskEx.CompletedTask;
            }

            var valid = true;
            if (_claimValueSplit != null)
                valid = principal.HasClaim(x => x.Type == this.ClaimType && _claimValueSplit.Contains(x.Value));
            else if (!string.IsNullOrEmpty(this.ClaimType))
                valid = principal.HasClaim(x => x.Type == this.ClaimType);
            if (valid && _roleSplit != null)
                valid = _roleSplit.Any(principal.IsInRole);

            if (!valid)
            {
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Forbidden);
                return TaskEx.CompletedTask;
            }

            //User is Authorized, complete execution
            return TaskEx.CompletedTask;

        }
    }
}
