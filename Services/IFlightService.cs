using AeroCloud.PPS.DTOs;

namespace AeroCloud.PPS.Services;

public interface IFlightService
{
    Task<IEnumerable<FlightResponse>> GetAllAsync();
    Task<FlightResponse?> GetByFlightNumberAsync(string flightNumber);

    /// <summary>
    /// Returns the full passenger manifest for a flight, with each passenger's
    /// bags included, ordered by seat number then name.
    /// </summary>
    Task<IEnumerable<ManifestEntry>?> GetManifestAsync(string flightNumber);

    /// <summary>
    /// Returns aggregate check-in and baggage statistics for a flight.
    /// </summary>
    Task<FlightStatsResponse?> GetStatsAsync(string flightNumber);
}
