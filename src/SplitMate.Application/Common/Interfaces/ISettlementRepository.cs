using SplitMate.Domain.Entities;

namespace SplitMate.Application.Common.Interfaces;

public interface ISettlementRepository
{
    Task<IReadOnlyList<Settlement>> GetForGroupAsync(Guid groupId, CancellationToken ct = default);
    Task AddAsync(Settlement settlement, CancellationToken ct = default);
}
