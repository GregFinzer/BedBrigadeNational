﻿using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IScheduleDataService : IRepository<Schedule>
    {
        Task<ServiceResponse<List<Schedule>>> GetSchedulesByLocationId(int locationId);
        Task<ServiceResponse<List<Schedule>>> GetFutureSchedulesByLocationId(int locationId);
        Task<ServiceResponse<List<Schedule>>> GetAvailableSchedulesByLocationId(int locationId);
    }
}