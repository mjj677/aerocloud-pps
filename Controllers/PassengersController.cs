using AeroCloud.PPS.DTOs;
using AeroCloud.PPS.Filters;
using AeroCloud.PPS.Services;
using Microsoft.AspNetCore.Mvc;

namespace AeroCloud.PPS.Controllers;

/// <summary>
/// Handles passenger lookup, check-in, and boarding operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PassengersController : ControllerBase
{
    private readonly IPassengerService _passengerService;
    private readonly ILogger<PassengersController> _logger;

    public PassengersController(IPassengerService passengerService, ILogger<PassengersController> logger)
    {
        _passengerService = passengerService;
        _logger = logger;
    }

    /// <summary>
    /// Look up a passenger by their booking reference (PNR).
    /// The ValidateBookingReferenceFilter runs before this action and rejects
    /// malformed references with a 400 before we even hit the service layer.
    /// </summary>
    [HttpGet("{bookingReference}")]
    [ServiceFilter(typeof(ValidateBookingReferenceFilter))]
    [ProducesResponseType(typeof(PassengerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPassenger(string bookingReference)
    {
        var passenger = await _passengerService.GetByBookingReferenceAsync(bookingReference);

        return passenger is null
            ? NotFound(new ErrorResponse("Passenger not found", $"No passenger found for booking reference '{bookingReference}'."))
            : Ok(passenger);
    }

    /// <summary>
    /// Check a passenger in. Allocates their seat and sets status to CheckedIn.
    /// Request body is validated by ASP.NET's model validation pipeline using
    /// Data Annotations on CheckInRequest — malformed requests are rejected with 400
    /// before reaching the service.
    /// </summary>
    [HttpPost("check-in")]
    [ProducesResponseType(typeof(PassengerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request)
    {
        // If model validation fails (Data Annotations on CheckInRequest),
        // ASP.NET returns a 400 automatically before this code runs.
        try
        {
            var result = await _passengerService.CheckInAsync(request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse("Passenger not found", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new ErrorResponse("Check-in not permitted", ex.Message));
        }
    }

    /// <summary>
    /// Mark a checked-in passenger as boarded at the gate.
    /// The ValidateBookingReferenceFilter validates the format before the action runs.
    /// </summary>
    [HttpPatch("{bookingReference}/board")]
    [ServiceFilter(typeof(ValidateBookingReferenceFilter))]
    [ProducesResponseType(typeof(PassengerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Board(string bookingReference)
    {
        try
        {
            var result = await _passengerService.UpdateBoardingStatusAsync(bookingReference);

            return result is null
                ? NotFound(new ErrorResponse("Passenger not found", $"No passenger found for booking reference '{bookingReference}'."))
                : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new ErrorResponse("Boarding not permitted", ex.Message));
        }
    }
}
