using Adviehchin.Domain.Entities;

namespace Adviehchin.Domain.Interfaces;

public interface INumberSequenceRepository
{
    Task<NumberSequence?> GetAsync(string name);

    Task AddAsync(NumberSequence sequence);
}
