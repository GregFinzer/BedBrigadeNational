using BedBrigade.Data.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
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
        var result = await _context.Configurations.ToListAsync();
        if (result != null)
        {
            return new ServiceResponse<List<Configuration>>($"Found {result.Count} records.", true, result);
        }
        return new ServiceResponse<List<Configuration>>("None found.");
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(string configurationkey)
    {
        var config = await _context.Configurations.FindAsync(configurationkey);
        if (config == null)
        {
            return new ServiceResponse<bool>($"Configuration record with key {configurationkey} not found");
        }
        try
        {
            _context.Configurations.Remove(config);
            await _context.SaveChangesAsync();
            return new ServiceResponse<bool>($"Removed record with key {configurationkey}.", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<bool>($"DB error on delete of configuration record with key {configurationkey} - {ex.Message} ({ex.ErrorCode})");
        }
    }

    public async Task<ServiceResponse<Configuration>> UpdateAsync(Configuration configuration)
    {
        var config = await _context.Configurations.FindAsync(configuration.ConfigurationKey);
        config.ConfigurationValue = configuration.ConfigurationValue;
        var result = await Task.Run(() => _context.Configurations.Update(config));
        if (result != null)
        {
            try
            { 
                await _context.SaveChangesAsync();
            }
            catch(DbException ex)
            {
                Log.Logger.Error("Database exception {0}", ex.ToString());                
            }
            catch(Exception ex)
            {
                Log.Logger.Error("Error saving configuration {0} ", ex.Message);
            }
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



