using AeroCloud.PPS.DTOs;
using AeroCloud.PPS.Services;
using Microsoft.AspNetCore.Mvc;

namespace AeroCloud.PPS.Controllers;

/// <summary>
/// Handles bag registration at the bag drop desk.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BagDropController : ControllerBase
{
    private readonly IBagDropService _bagDropService;

    public BagDropController(IBagDropService bagDropService)
    {
        _bagDropService = bagDropService;
    }

    /// <summary>List all bags registered to a passenger.</summary>
    /// <param name="passengerId">Internal passenger ID</param>
    [HttpGet("passenger/{passengerId:int}")]
    [ProducesResponseType(typeof(IEnumerable<BagDropResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBagsForPassenger(int passengerId)
    {
        var bags = await _bagDropService.GetBagsForPassengerAsync(passengerId);
        return Ok(bags);
    }

    /// <summary>Register a new bag at bag drop for a checked-in passenger.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BagDropResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegisterBag([FromBody] BagDropRequest request)
    {
        try
        {
            var bag = await _bagDropService.RegisterBagAsync(request);
            return CreatedAtAction(
                nameof(GetBagsForPassenger),
                new { passengerId = request.PassengerId },
                bag);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse("Passenger not found", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new ErrorResponse("Bag registration not permitted", ex.Message));
        }
        catch (ArgumentException ex)
        {
            return UnprocessableEntity(new ErrorResponse("Invalid bag data", ex.Message));
        }
    }
}
