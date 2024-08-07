﻿using System.Security.Claims;

namespace BedBrigade.Client.Services
{
    public interface IAuthService
    {
        ClaimsPrincipal CurrentUser { get; set; }
        bool IsLoggedIn { get; }
        event Action<ClaimsPrincipal> UserChanged;
        Task<bool> GetStateFromTokenAsync();
        Task LogoutAsync(); 
        Task Login(ClaimsPrincipal user);
    }
}
