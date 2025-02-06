// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Mocha.Query.Prometheus.DTOs;
using Mocha.Query.Prometheus.PromQL.Exceptions;

namespace Mocha.Query.Prometheus.Controllers;

public class PrometheusExceptionFilter(ILogger<PrometheusExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is PromQLIllegalExpressionException ex)
        {
            context.Result = new ObjectResult(new QueryResponse<ResponseData>
            {
                Status = ResultStatus.Error,
                ErrorType = ErrorType.BadData,
                Error = "Invalid query: " + ex.Message
            })
            { StatusCode = (int)HttpStatusCode.BadRequest };
        }
        else
        {
            logger.LogError(context.Exception, "Error while executing query.");
            var errorType = context.Exception is PromQLBadRequestException ? ErrorType.BadData : ErrorType.Internal;

            context.Result = new ObjectResult(new QueryResponse<ResponseData>
            {
                Status = ResultStatus.Error,
                ErrorType = errorType,
                Error = context.Exception.Message
            })
            { StatusCode = (int)HttpStatusCode.InternalServerError };
        }

        context.ExceptionHandled = true;
    }
}
