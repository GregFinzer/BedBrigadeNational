using BedBrigade.Common.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.Common.Logic
{
    public static class ClaimsPrincipalHelper
    {
        public static string? GetUserName(ClaimsPrincipal identity)
        {
            if (identity == null)
            {
                return string.Empty;
            }

            return identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? Defaults.DefaultUserNameAndEmail;
        }

        public static int? GetLocationId(ClaimsPrincipal identity)
        {
            if (identity == null)
            {
                return null;
            }

            return int.TryParse(identity.Claims.FirstOrDefault(c => c.Type == "LocationId")?.Value, out int locationId) ? locationId : null;
        }
    }
}
