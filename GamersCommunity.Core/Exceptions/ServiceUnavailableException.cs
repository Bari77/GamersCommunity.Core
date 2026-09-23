using System.Net;

namespace GamersCommunity.Core.Exceptions;

/// <summary>
/// Thrown when a dependent microservice queue is missing or has no active consumer.
/// </summary>
public class ServiceUnavailableException(
    string code = "UNAVAILABLE",
    string? message = "The microservice is unavailable.")
    : AppException(HttpStatusCode.ServiceUnavailable, code, message);
