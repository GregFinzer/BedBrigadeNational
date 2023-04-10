
using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System.Data.Common;
using System.Security.Claims;

namespace BedBrigade.Data.Services;

public class DonationDataService : IDonationDataService
{

    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly AuthenticationStateProvider _auth;

    protected ClaimsPrincipal _identity;

    public DonationDataService(IDbContextFactory<DataContext> dbContextFactory, AuthenticationStateProvider authProvider)
    {
        _contextFactory = dbContextFactory;
        _auth = authProvider;
    }

    public async Task<ServiceResponse<Donation>> GetAsync(int donationId)
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            var result = await context.Donations.FindAsync(donationId);
            if (result != null)
            {
                return new ServiceResponse<Donation>("Found Record", true, result);
            }

            return new ServiceResponse<Donation>("Not Found");
        }
    }

    public async Task<ServiceResponse<List<Donation>>> GetAllAsync()
    {
        var authState = await _auth.GetAuthenticationStateAsync();

        var role = authState.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role).Value;

        using (var context = _contextFactory.CreateDbContext())
        {

            List<Donation> result;
            if (role.ToLower() != "national admin")
            {
                int.TryParse(authState.User.Claims.FirstOrDefault(c => c.Type == "LocationId").Value ?? "0", out int locationId);
                result = context.Donations.Where(u => u.LocationId == locationId).ToList();
            }
            else
            {
                result = await context.Donations.ToListAsync();
            }

            if (result != null)
            {
                return new ServiceResponse<List<Donation>>($"Found {result.Count} records.", true, result);
            }
        }
        return new ServiceResponse<List<Donation>>("None found.");
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(int donationId)
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            var user = await context.Users.FindAsync(donationId);
            if (user == null)
            {
                return new ServiceResponse<bool>($"Donation record with key {donationId} not found");
            }
            try
            {
                context.Users.Remove(user);
                await context.SaveChangesAsync();
                return new ServiceResponse<bool>($"Removed record with key {donationId}.", true);
            }
            catch (DbException ex)
            {
                return new ServiceResponse<bool>($"DB error on delete of user record with key {donationId} - {ex.Message} ({ex.ErrorCode})");
            }
        }
    }

    public async Task<ServiceResponse<Donation>> UpdateAsync(Donation donation)
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            var entity = await context.Users.FindAsync(donation.DonationId);

            if (entity != null)
            {
                context.Entry(entity).CurrentValues.SetValues(donation);
                context.Entry(entity).State = EntityState.Modified;
                await context.SaveChangesAsync();
            }
            return new ServiceResponse<Donation>($"Donation record was updated.", true, donation);
        }
        return new ServiceResponse<Donation>($"Donation with key {donation.DonationId} was not updated.");
    }

    public async Task<ServiceResponse<Donation>> CreateAsync(Donation donation)
    {
        using (var context = _contextFactory.CreateDbContext())
        {

            try
            {
                await context.Donations.AddAsync(donation);
                await context.SaveChangesAsync();
                return new ServiceResponse<Donation>($"Added location with key {donation.DonationId}", true);
            }
            catch (DbException ex)
            {
                return new ServiceResponse<Donation>($"DB error on delete of user record with key {donation.DonationId} - {ex.Message} ({ex.ErrorCode})");
            }
        }

    }


}



