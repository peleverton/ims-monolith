using IMS.Modular.Modules.BinPacking.Domain.Entities;

namespace IMS.Modular.Modules.BinPacking.Domain;

public interface IPackagingTypeRepository
{
    Task<List<PackagingType>> GetActiveOrderedByVolumeAsync(CancellationToken ct = default);
    Task<List<PackagingType>> GetAllAsync(CancellationToken ct = default);
    Task<PackagingType?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(PackagingType type, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
