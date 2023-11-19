using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NEnvoy.Models;
using Refit;

#nullable enable

namespace NEnvoy.Internals;

public interface IEnvoyJsonApi
{
    [Get("/production.json")]
    Task<ProductionData> GetProductionAsync(CancellationToken cancellationToken = default);

    [Get("/inventory.json")]
    Task<IEnumerable<InventoryItem>> GetInventoryAsync(CancellationToken cancellationToken = default);

    [Get("/api/v1/production/inverters")]
    Task<IEnumerable<V1Inverter>> GetV1InvertersAsync(CancellationToken cancellationToken = default);
}