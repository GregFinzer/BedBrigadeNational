﻿using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface IUserDataService
    {
        Task<ServiceResponse<User>> CreateAsync(User user);
        Task<ServiceResponse<bool>> DeleteAsync(string UserName);
        Task<ServiceResponse<List<User>>> GetAllAsync();
        Task<ServiceResponse<User>> GetAsync(string UserName);
        Task<ServiceResponse<User>> UpdateAsync(User user);
        Task<ServiceResponse<bool>> UserExistsAsync(string email);
    }
}