using System.Xml;
using Microsoft.AspNetCore.Diagnostics;

namespace Tides.Api.Kartverket;

public sealed class KartverketExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<KartverketExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (!IsKartverketFailure(exception))
        {
            return false;
        }

        logger.LogWarning(exception, "Request to Kartverket's Tide API failed.");

        httpContext.Response.StatusCode = StatusCodes.Status502BadGateway;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = StatusCodes.Status502BadGateway,
                Title = "Kartverket's Tide API is unavailable.",
            },
        });
    }

    private static bool IsKartverketFailure(Exception exception) =>
        exception is HttpRequestException
            or KartverketException
            or XmlException
            or TaskCanceledException { InnerException: TimeoutException };
}
