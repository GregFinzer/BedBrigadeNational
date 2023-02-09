using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.Data.Services
{
    public abstract class BaseDataService
    {
        protected ClaimsPrincipal _identity { get; private set; }
        protected BaseDataService(AuthenticationStateProvider authProvider)
        {
            //GetUserClaims(authProvider);

        }

    }
}
