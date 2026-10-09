using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Common;
using Services.Ports;

namespace api.Errors
{
    // Maps service/persistence exceptions to ProblemDetails responses, in one place.
    public class ApiExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetails;

        public ApiExceptionHandler(IProblemDetailsService problemDetails)
        {
            _problemDetails = problemDetails;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
        {
            ProblemDetails? problem = exception switch
            {
                ValidationException e => new ValidationProblemDetails(e.Errors.ToDictionaryCopy()) { Status = StatusCodes.Status400BadRequest },
                NotFoundException e => new ProblemDetails { Status = StatusCodes.Status404NotFound, Detail = e.Message },
                ConcurrencyConflictException or UniqueConstraintException or ReferenceConstraintException =>
                    new ProblemDetails { Status = StatusCodes.Status409Conflict, Detail = exception.Message },
                // "type" lets the UI react, e.g. open the API key settings.
                AiNotConfiguredException => new ProblemDetails { Status = StatusCodes.Status400BadRequest, Type = "ai-not-configured", Detail = exception.Message },
                AiRateLimitedException => new ProblemDetails { Status = StatusCodes.Status429TooManyRequests, Type = "ai-rate-limited", Detail = exception.Message },
                AiRequestException => new ProblemDetails { Status = StatusCodes.Status502BadGateway, Type = "ai-failed", Detail = exception.Message },
                _ => null,
            };
            if (problem == null)
                return false;

            context.Response.StatusCode = problem.Status!.Value;
            return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = problem,
                Exception = exception,
            });
        }
    }

    internal static class DictionaryExtensions
    {
        public static System.Collections.Generic.Dictionary<string, string[]> ToDictionaryCopy(
            this System.Collections.Generic.IReadOnlyDictionary<string, string[]> source) =>
            new(source);
    }
}
