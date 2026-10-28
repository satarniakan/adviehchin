using Microsoft.EntityFrameworkCore;
using Adviehchin.Domain.Entities;
using Adviehchin.Domain.Interfaces;
using Adviehchin.Infrastructure.Data;

namespace Adviehchin.Infrastructure.Repositories;

public class PushSubscriptionRepository : IPushSubscriptionRepository
{
    private readonly AdviehchinDbContext _context;

    public PushSubscriptionRepository(AdviehchinDbContext context) => _context = context;

    public Task<List<PushSubscription>> GetForUserAsync(string userId) =>
        _context.PushSubscriptions.Where(s => s.UserId == userId).ToListAsync();

    public Task<PushSubscription?> GetByEndpointAsync(string userId, string endpoint) =>
        _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == endpoint);

    public Task<PushSubscription?> GetByEndpointGlobalAsync(string endpoint) =>
        _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == endpoint);

    public async Task AddAsync(PushSubscription subscription)
    {
        await _context.PushSubscriptions.AddAsync(subscription);
    }

    public Task RemoveAsync(PushSubscription subscription)
    {
        _context.PushSubscriptions.Remove(subscription);
        return Task.CompletedTask;
    }

    public Task<int> RemoveByEndpointAsync(string endpoint) =>
        _context.PushSubscriptions.Where(s => s.Endpoint == endpoint).ExecuteDeleteAsync();
}
