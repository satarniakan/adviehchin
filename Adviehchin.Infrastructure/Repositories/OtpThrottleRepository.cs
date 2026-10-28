using Microsoft.EntityFrameworkCore;
using Adviehchin.Domain.Entities;
using Adviehchin.Domain.Interfaces;
using Adviehchin.Infrastructure.Data;

namespace Adviehchin.Infrastructure.Repositories;

public class OtpThrottleRepository : IOtpThrottleRepository
{
    private readonly AdviehchinDbContext _context;

    public OtpThrottleRepository(AdviehchinDbContext context) => _context = context;

    public Task<OtpThrottle?> GetByPhoneAsync(string phoneNumber) =>
        _context.OtpThrottles.FirstOrDefaultAsync(t => t.PhoneNumber == phoneNumber);

    public async Task AddAsync(OtpThrottle throttle)
    {
        await _context.OtpThrottles.AddAsync(throttle);
    }

    public Task<int> DeleteStaleAsync(DateTime cutoffUtc) =>
        _context.OtpThrottles
            .Where(t => (t.FailedWindowStartUtc == null || t.FailedWindowStartUtc < cutoffUtc)
                     && (t.GenerationWindowStartUtc == null || t.GenerationWindowStartUtc < cutoffUtc))
            .ExecuteDeleteAsync();
}
