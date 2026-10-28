using Microsoft.EntityFrameworkCore;
using Adviehchin.Domain.Entities;
using Adviehchin.Domain.Interfaces;
using Adviehchin.Infrastructure.Data;

namespace Adviehchin.Infrastructure.Repositories;

public class NumberSequenceRepository : INumberSequenceRepository
{
    private readonly AdviehchinDbContext _context;

    public NumberSequenceRepository(AdviehchinDbContext context) => _context = context;

    public Task<NumberSequence?> GetAsync(string name) =>
        _context.NumberSequences.FirstOrDefaultAsync(s => s.Name == name);

    public async Task AddAsync(NumberSequence sequence)
    {
        await _context.NumberSequences.AddAsync(sequence);
    }
}
