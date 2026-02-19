using AeroCloud.PPS.DTOs;
using AeroCloud.PPS.Services;
using Microsoft.AspNetCore.Mvc;

namespace AeroCloud.PPS.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FlightsController : ControllerBase
{
    private readonly IFlightService _flightService;

    public FlightsController(IFlightService flightService)
    {
        _flightService = flightService;
    }

    /// <summary>Return all flights currently tracked in the system.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FlightResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var flights = await _flightService.GetAllAsync();
        return Ok(flights);
    }

    /// <summary>Look up a specific flight by its IATA flight number.</summary>
    [HttpGet("{flightNumber}")]
    [ProducesResponseType(typeof(FlightResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFlight(string flightNumber)
    {
        var flight = await _flightService.GetByFlightNumberAsync(flightNumber);
        return flight is null
            ? NotFound(new ErrorResponse("Flight not found", $"No flight found with number '{flightNumber}'."))
            : Ok(flight);
    }

    /// <summary>
    /// Full passenger manifest for a flight — ordered by seat, each passenger
    /// includes their registered bags. Built with LINQ Include + OrderBy + Select.
    /// </summary>
    [HttpGet("{flightNumber}/manifest")]
    [ProducesResponseType(typeof(IEnumerable<ManifestEntry>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifest(string flightNumber)
    {
        var manifest = await _flightService.GetManifestAsync(flightNumber);
        return manifest is null
            ? NotFound(new ErrorResponse("Flight not found", $"No flight found with number '{flightNumber}'."))
            : Ok(manifest);
    }

    /// <summary>
    /// Aggregate check-in and baggage statistics for a flight.
    /// Computed with LINQ GroupBy, Sum, Count and All.
    /// </summary>
    [HttpGet("{flightNumber}/stats")]
    [ProducesResponseType(typeof(FlightStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStats(string flightNumber)
    {
        var stats = await _flightService.GetStatsAsync(flightNumber);
        return stats is null
            ? NotFound(new ErrorResponse("Flight not found", $"No flight found with number '{flightNumber}'."))
            : Ok(stats);
    }
}
