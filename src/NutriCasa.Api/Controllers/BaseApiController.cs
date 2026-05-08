using Microsoft.AspNetCore.Mvc;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "UNAUTHORIZED" => Unauthorized(new ApiError { Code = result.ErrorCode, Message = result.Error! }),
                "NOT_FOUND" => NotFound(new ApiError { Code = result.ErrorCode, Message = result.Error! }),
                "FORBIDDEN" => StatusCode(403, new ApiError { Code = result.ErrorCode, Message = result.Error! }),
                _ => BadRequest(new ApiError { Code = result.ErrorCode ?? "ERROR", Message = result.Error! })
            };
        }

        if (result.Value is null)
            return Ok();

        return Ok(result.Value);
    }

    protected IActionResult HandleResult(Result result)
    {
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "UNAUTHORIZED" => Unauthorized(new ApiError { Code = result.ErrorCode, Message = result.Error! }),
                "NOT_FOUND" => NotFound(new ApiError { Code = result.ErrorCode, Message = result.Error! }),
                "FORBIDDEN" => StatusCode(403, new ApiError { Code = result.ErrorCode, Message = result.Error! }),
                _ => BadRequest(new ApiError { Code = result.ErrorCode ?? "ERROR", Message = result.Error! })
            };
        }

        return Ok(new { message = "ok" });
    }
}
