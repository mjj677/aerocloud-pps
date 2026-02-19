using AeroCloud.PPS.DTOs;

namespace AeroCloud.PPS.Services;

public interface IPassengerService
{
    Task<PassengerResponse?> GetByBookingReferenceAsync(string bookingReference);
    Task<PassengerResponse> CheckInAsync(CheckInRequest request);
    Task<PassengerResponse?> UpdateBoardingStatusAsync(string bookingReference);

    /// <summary>
    /// Search passengers using any combination of optional filters.
    /// Results are ordered alphabetically by full name.
    /// </summary>
    Task<IEnumerable<PassengerResponse>> SearchAsync(PassengerSearchQuery query);
}
