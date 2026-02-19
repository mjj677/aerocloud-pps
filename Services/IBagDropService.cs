using AeroCloud.PPS.DTOs;

namespace AeroCloud.PPS.Services;

public interface IBagDropService
{
    Task<IEnumerable<BagDropResponse>> GetBagsForPassengerAsync(int passengerId);
    Task<BagDropResponse> RegisterBagAsync(BagDropRequest request);
}
