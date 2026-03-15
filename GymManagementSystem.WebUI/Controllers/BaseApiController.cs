using GymManagementSystem.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected ActionResult<ApiResponse<T>> ApiOk<T>(T data, string message = "Success")
    {
        return Ok(ApiResponse<T>.Ok(data, message, StatusCodes.Status200OK));
    }

    protected ActionResult<ApiResponse<T>> ApiCreated<T>(T data, string message = "Created")
    {
        return StatusCode(StatusCodes.Status201Created, ApiResponse<T>.Ok(data, message, StatusCodes.Status201Created));
    }

    protected ActionResult<ApiResponse<T>> ApiBadRequest<T>(string message, IEnumerable<string>? errors = null)
    {
        return BadRequest(ApiResponse<T>.Fail(message, StatusCodes.Status400BadRequest, errors));
    }

    protected ActionResult<ApiResponse<T>> ApiUnauthorized<T>(string message, IEnumerable<string>? errors = null)
    {
        return Unauthorized(ApiResponse<T>.Fail(message, StatusCodes.Status401Unauthorized, errors));
    }

    protected ActionResult<ApiResponse<T>> ApiNotFound<T>(string message, IEnumerable<string>? errors = null)
    {
        return NotFound(ApiResponse<T>.Fail(message, StatusCodes.Status404NotFound, errors));
    }

    protected ActionResult<ApiResponse<T>> ApiError<T>(string message, int statusCode, IEnumerable<string>? errors = null)
    {
        return StatusCode(statusCode, ApiResponse<T>.Fail(message, statusCode, errors));
    }
}