using BedBrigade.Data.Models;
using Microsoft.EntityFrameworkCore;
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
        var result = await _context.BedRequests.ToListAsync();
        if (result != null)
        {
            return new ServiceResponse<List<BedRequest>>($"Found {result.Count} records.", true, result);
        }
        return new ServiceResponse<List<BedRequest>>("None found.");
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(int BedRequestId)
    {
        var bedRequest = await _context.BedRequests.FindAsync(BedRequestId);
        if (bedRequest == null)
        {
            return new ServiceResponse<bool>($"BedRequest record with key {BedRequestId} not found");
        }
        try
        {
            _context.BedRequests.Remove(bedRequest);
            await _context.SaveChangesAsync();
            return new ServiceResponse<bool>($"Removed record with key {BedRequestId}.", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<bool>($"DB error on delete of Bed Request record with key {BedRequestId} - {ex.Message} ({ex.ErrorCode})");
        }
    }

    public async Task<ServiceResponse<BedRequest>> UpdateAsync(BedRequest bedRequest)
    {
        var entity = _context.BedRequests.Find(bedRequest.BedRequestId);
        if (entity != null)
        {
            _context.Entry(entity).CurrentValues.SetValues(bedRequest);
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChangesAsync();
            return new ServiceResponse<BedRequest>($"Updated bedRequest with key {bedRequest.BedRequestId}", true);
        }
        return new ServiceResponse<BedRequest>($"Bed Request with key {bedRequest.BedRequestId} was not updated.");
    }

    public async Task<ServiceResponse<BedRequest>> CreateAsync(BedRequest bedRequest)
    {
        try
        {
            await _context.BedRequests.AddAsync(bedRequest);
            await _context.SaveChangesAsync();
            return new ServiceResponse<BedRequest>($"Added Bed Request with key {bedRequest.BedRequestId}", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<BedRequest>($"DB error on delete of Bed Request record with key {bedRequest.BedRequestId} - {ex.Message} ({ex.ErrorCode})");
        }

    }
}



