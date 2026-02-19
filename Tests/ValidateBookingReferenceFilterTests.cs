using AeroCloud.PPS.DTOs;
using AeroCloud.PPS.Filters;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;


namespace AeroCloud.PPS.Tests;

/// <summary>
/// Tests for ValidateBookingReferenceFilter.
/// Verifies the filter short-circuits with 400 for invalid PNR formats
/// and passes through for valid ones.
/// </summary>
public class ValidateBookingReferenceFilterTests
{
    private readonly ValidateBookingReferenceFilter _sut =
        new(NullLogger<ValidateBookingReferenceFilter>.Instance);

    private static ActionExecutingContext BuildContext(string? bookingReference)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        var args = new Dictionary<string, object?>();
        if (bookingReference is not null)
            args["bookingReference"] = bookingReference;

        return new ActionExecutingContext(
            actionContext,
            filters: new List<IFilterMetadata>(),
            actionArguments: args,
            controller: new object());
    }

    [Theory]
    [InlineData("ABC123")]
    [InlineData("abc123")]
    [InlineData("A1B2C3")]
    [InlineData("999999")]
    public void OnActionExecuting_DoesNotSetResult_ForValidPnr(string validRef)
    {
        var context = BuildContext(validRef);
        _sut.OnActionExecuting(context);
        context.Result.Should().BeNull("valid PNR should pass through");
    }

    [Theory]
    [InlineData("AB12")]
    [InlineData("ABCDEFG")]
    [InlineData("ABC-12")]
    [InlineData("ABC 12")]
    [InlineData("")]
    public void OnActionExecuting_SetsBadRequest_ForInvalidPnrFormat(string invalidRef)
    {
        var context = BuildContext(invalidRef);
        _sut.OnActionExecuting(context);
        context.Result.Should().BeOfType<BadRequestObjectResult>();
        var result = (BadRequestObjectResult)context.Result!;
        var error = result.Value.Should().BeOfType<ErrorResponse>().Subject;
        error.Error.Should().Contain("Invalid");
    }

    [Fact]
    public void OnActionExecuting_DoesNothing_WhenNoBookingReferenceArgument()
    {
        var context = BuildContext(null);
        _sut.OnActionExecuting(context);
        context.Result.Should().BeNull();
    }
}
