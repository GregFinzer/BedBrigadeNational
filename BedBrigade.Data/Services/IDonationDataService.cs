﻿using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface IDonationDataService
    {
        Task<ServiceResponse<Donation>> CreateAsync(Donation donation);
        Task<ServiceResponse<bool>> DeleteAsync(int donationId);
        Task<ServiceResponse<List<Donation>>> GetAllAsync();
        Task<ServiceResponse<Donation>> GetAsync(int donationId);
        Task<ServiceResponse<Donation>> UpdateAsync(Donation donation);
    }
}