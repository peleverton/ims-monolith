using IMS.Modular.Modules.BinPacking.Domain;
using IMS.Modular.Modules.BinPacking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IMS.Modular.Modules.BinPacking.Infrastructure;

public sealed class PackagingTypeRepository(BinPackingDbContext db) : IPackagingTypeRepository
{
    public async Task<List<PackagingType>> GetActiveOrderedByVolumeAsync(CancellationToken ct = default)
        => await db.PackagingTypes
            .Where(p => p.IsActive)
            .OrderBy(p => (double)(p.MaxLengthCm * p.MaxWidthCm * p.MaxHeightCm))
            .ToListAsync(ct);

    public async Task<List<PackagingType>> GetAllAsync(CancellationToken ct = default)
        => await db.PackagingTypes
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

    public async Task<PackagingType?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.PackagingTypes.FindAsync([id], ct);

    public async Task AddAsync(PackagingType type, CancellationToken ct = default)
        => await db.PackagingTypes.AddAsync(type, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
