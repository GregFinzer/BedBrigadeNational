using BedBrigade.Data.Models;


namespace BedBrigade.Data.Services
{
    public interface IContactUsDataService : IRepository<ContactUs>
    {
        Task<ServiceResponse<List<ContactUs>>> GetAllForLocationAsync();
    }
}
