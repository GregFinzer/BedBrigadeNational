using BedBrigade.Data.Models;
using System.Data.Common;

namespace BedBrigade.Data.Services;

public class ConfigurationDataService : IConfigurationDataService
{
    private readonly DataContext _context;
    public ConfigurationDataService(DataContext context)
    {
        _context = context;
    }

    public async Task<ServiceResponse<Configuration>> GetAsync(string configurationkey)
    {
        var result = await _context.Configurations.FindAsync(configurationkey);
        if (result != null)
        {
            return new ServiceResponse<Configuration>("Found Record", true, result);
        }
        return new ServiceResponse<Configuration>("Not Found");
    }

    public async Task<ServiceResponse<List<Configuration>>> GetAllAsync()
    {
        var result = _context.Configurations.ToList();
        if (result != null)
        {
            return new ServiceResponse<List<Configuration>>($"Found {result.Count} records.", true, result);
        }
        return new ServiceResponse<List<Configuration>>("None found.");
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(string configurationkey)
    {
        var user = await _context.Users.FindAsync(configurationkey);
        if (user == null)
        {
            return new ServiceResponse<bool>($"User record with key {configurationkey} not found");
        }
        try
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return new ServiceResponse<bool>($"Removed record with key {configurationkey}.", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<bool>($"DB error on delete of user record with key {configurationkey} - {ex.Message} ({ex.ErrorCode})");
        }
    }

    public async Task<ServiceResponse<Configuration>> UpdateAsync(Configuration configuration)
    {
        var result = _context.Configurations.Update(configuration);
        if (result != null)
        {
            return new ServiceResponse<Configuration>($"Updated location with key {configuration}", true);
        }
        return new ServiceResponse<Configuration>($"User with key {configuration} was not updated.");
    }

    public async Task<ServiceResponse<Configuration>> CreateAsync(Configuration configuration)
    {
        try
        {
            await _context.Configurations.AddAsync(configuration);
            await _context.SaveChangesAsync();
            return new ServiceResponse<Configuration>($"Added configuration with key {configuration.ConfigurationKey}", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<Configuration>($"DB error on delete of user record with key {configuration.ConfigurationKey} - {ex.Message} ({ex.ErrorCode})");
        }

    }


}



