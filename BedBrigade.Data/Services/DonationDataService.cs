﻿
using BedBrigade.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace BedBrigade.Data.Services;

public class DonationDataService : IDonationDataService
{

    private readonly DataContext _context;

    public DonationDataService(DataContext context)
    {
        _context = context;
    }

    public async Task<ServiceResponse<Donation>> GetAsync(int donationId)
    {
        var result = await _context.Donations.FindAsync(donationId);
        if (result != null)
        {
            return new ServiceResponse<Donation>("Found Record", true, result);
        }
        return new ServiceResponse<Donation>("Not Found");
    }

    public async Task<ServiceResponse<List<Donation>>> GetAllAsync()
    {
        var result = await _context.Donations.ToListAsync();
        if (result != null)
        {
            return new ServiceResponse<List<Donation>>($"Found {result.Count} records.", true, result);
        }
        return new ServiceResponse<List<Donation>>("None found.");
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(int donationId)
    {
        var user = await _context.Users.FindAsync(donationId);
        if (user == null)
        {
            return new ServiceResponse<bool>($"Donation record with key {donationId} not found");
        }
        try
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return new ServiceResponse<bool>($"Removed record with key {donationId}.", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<bool>($"DB error on delete of user record with key {donationId} - {ex.Message} ({ex.ErrorCode})");
        }
    }

    public async Task<ServiceResponse<Donation>> UpdateAsync(Donation donation)
    {
        var result = await Task.Run(() =>_context.Donations.Update(donation));
        if (result != null)
        {
            return new ServiceResponse<Donation>($"Updated donation with key {donation.DonationId}", true);
        }
        return new ServiceResponse<Donation>($"User with key {donation.DonationId} was not updated.");
    }

    public async Task<ServiceResponse<Donation>> CreateAsync(Donation donation)
    {
        try
        {
            await _context.Donations.AddAsync(donation);
            await _context.SaveChangesAsync();
            return new ServiceResponse<Donation>($"Added location with key {donation.DonationId}", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<Donation>($"DB error on delete of user record with key {donation.DonationId} - {ex.Message} ({ex.ErrorCode})");
        }

    }


}



