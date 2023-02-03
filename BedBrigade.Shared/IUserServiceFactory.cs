using BedBrigade.Shared;

namespace BedBrigade.Client;

public interface IUserServiceFactory
{
    IUserService Create();
}