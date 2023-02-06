using BedBrigade.Data.Models;
using System.Data.Common;

namespace BedBrigade.Data.Services;

public class BedRequestDataService : IBedRequestDataService
{
    private readonly DataContext _context;

    public BedRequestDataService(DataContext context)
    {
        _context = context;
    }

    public async Task<ServiceResponse<BedRequest>> GetAsync(int bedRequestId)
    {
        var result = await _context.BedRequests.FindAsync(bedRequestId);
        if (result != null)
        {
            return new ServiceResponse<BedRequest>("Found Record", true, result);
        }
        return new ServiceResponse<BedRequest>("Not Found");
    }

    public async Task<ServiceResponse<List<BedRequest>>> GetAllAsync()
    {
        var result = _context.BedRequests.ToList();
        if (result != null)
        {
            return new ServiceResponse<List<BedRequest>>($"Found {result.Count} records.", true, result);
        }
        return new ServiceResponse<List<BedRequest>>("None found.");
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(string UserName)
    {
        var user = await _context.Users.FindAsync(UserName);
        if (user == null)
        {
            return new ServiceResponse<bool>($"User record with key {UserName} not found");
        }
        try
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return new ServiceResponse<bool>($"Removed record with key {UserName}.", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<bool>($"DB error on delete of user record with key {UserName} - {ex.Message} ({ex.ErrorCode})");
        }
    }

    public async Task<ServiceResponse<BedRequest>> UpdateAsync(BedRequest bedRequest)
    {
        var result = _context.BedRequests.Update(bedRequest);
        if (result != null)
        {
            return new ServiceResponse<BedRequest>($"Updated bedRequest with key {bedRequest.BedRequestId}", true);
        }
        return new ServiceResponse<BedRequest>($"User with key {bedRequest.BedRequestId} was not updated.");
    }

    public async Task<ServiceResponse<BedRequest>> CreateAsync(BedRequest bedRequest)
    {
        try
        {
            await _context.BedRequests.AddAsync(bedRequest);
            await _context.SaveChangesAsync();
            return new ServiceResponse<BedRequest>($"Added bedRequest with key {bedRequest.BedRequestId}", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<BedRequest>($"DB error on delete of user record with key {bedRequest.BedRequestId} - {ex.Message} ({ex.ErrorCode})");
        }

    }
}



