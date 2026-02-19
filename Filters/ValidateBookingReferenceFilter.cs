using AeroCloud.PPS.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.RegularExpressions;

namespace AeroCloud.PPS.Filters;

/// <summary>
/// ASP.NET Core action filter that validates any <c>bookingReference</c> route value
/// before the controller action runs.
///
/// IATA booking references (PNRs) are exactly 6 uppercase alphanumeric characters.
/// Centralising this here means we don't repeat the same guard clause across every
/// controller action that accepts a booking reference.
///
/// Usage — decorate an action or controller:
/// <code>
///   [ServiceFilter(typeof(ValidateBookingReferenceFilter))]
///   public async Task&lt;IActionResult&gt; GetPassenger(string bookingReference) { ... }
/// </code>
/// </summary>
public class ValidateBookingReferenceFilter : IActionFilter
{
    // IATA PNR: 6 alphanumeric characters (letters + digits, case-insensitive match)
    private static readonly Regex PnrPattern = new(@"^[A-Z0-9]{6}$", RegexOptions.Compiled);

    private readonly ILogger<ValidateBookingReferenceFilter> _logger;

    public ValidateBookingReferenceFilter(ILogger<ValidateBookingReferenceFilter> logger)
    {
        _logger = logger;
    }

    /// <summary>Runs before the action. Short-circuits with 400 if the ref is malformed.</summary>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Pull the booking reference from route values if present
        if (!context.ActionArguments.TryGetValue("bookingReference", out var rawRef)
            || rawRef is not string bookingReference)
        {
            return; // No booking reference in this action — nothing to validate
        }

        var normalised = bookingReference.ToUpperInvariant();

        if (!PnrPattern.IsMatch(normalised))
        {
            _logger.LogWarning(
                "Rejected request with invalid booking reference format: '{BookingReference}'",
                bookingReference);

            context.Result = new BadRequestObjectResult(
                new ErrorResponse(
                    "Invalid booking reference",
                    "Booking references must be exactly 6 alphanumeric characters (e.g. ABC123)."));
        }
    }

    /// <summary>Runs after the action — nothing to do here.</summary>
    public void OnActionExecuted(ActionExecutedContext context) { }
}
