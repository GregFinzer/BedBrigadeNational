using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services;

public interface IEmailQueueLogic
{
    Task<ServiceResponse<string>> QueueEmail(EmailQueue email);
    void ProcessQueue(object? state);
}