using System;
using System.Collections.Generic;
using System.Web.Http.Validation;

namespace WebApiExtensions.Services
{
    public class ExcludeModelValidator : DefaultBodyModelValidator
    {
        public readonly HashSet<Type> ExcludeTypes = new HashSet<Type>(); 

        public override bool ShouldValidateType(Type type)
        {
            return !ExcludeTypes.Contains(type) && base.ShouldValidateType(type);
        }
    }
}