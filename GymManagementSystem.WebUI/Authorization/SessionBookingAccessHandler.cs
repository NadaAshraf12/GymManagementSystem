using System.Security.Claims;
using System.Text.Json;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.WebUI.Authorization;

public class SessionBookingAccessHandler : AuthorizationHandler<SessionBookingAccessRequirement>
{
    private const string AdminRole = "Admin";
    private const string TrainerRole = "Trainer";
    private const string MemberRole = "Member";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionBookingAccessHandler(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, SessionBookingAccessRequirement requirement)
    {
        if (context.User.IsInRole(AdminRole))
        {
            context.Succeed(requirement);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        var (memberId, workoutSessionId) = await GetBookingInputsAsync(httpContext.Request);
        if (string.IsNullOrWhiteSpace(memberId) || workoutSessionId <= 0)
        {
            return;
        }

        if (context.User.IsInRole(MemberRole))
        {
            if (string.Equals(memberId, userId, StringComparison.Ordinal))
            {
                context.Succeed(requirement);
            }
            return;
        }

        if (context.User.IsInRole(TrainerRole))
        {
            var assignmentRepo = _unitOfWork.Repository<TrainerMemberAssignment>();
            var isAssigned = await assignmentRepo.Query()
                .AnyAsync(a => a.TrainerId == userId && a.MemberId == memberId);

            if (isAssigned)
            {
                context.Succeed(requirement);
                return;
            }

            var sessionRepo = _unitOfWork.Repository<WorkoutSession>();
            var ownsSession = await sessionRepo.Query()
                .AnyAsync(s => s.Id == workoutSessionId && s.TrainerId == userId);

            if (ownsSession)
            {
                context.Succeed(requirement);
            }
        }
    }

    private static async Task<(string? MemberId, int WorkoutSessionId)> GetBookingInputsAsync(HttpRequest request)
    {
        if (HttpMethods.IsPost(request.Method))
        {
            request.EnableBuffering();
            request.Body.Position = 0;
            using var doc = await JsonDocument.ParseAsync(request.Body);
            request.Body.Position = 0;

            var root = doc.RootElement;
            var memberId = root.TryGetProperty("memberId", out var memberIdElement)
                ? memberIdElement.GetString()
                : null;
            var workoutSessionId = root.TryGetProperty("workoutSessionId", out var sessionElement)
                ? sessionElement.GetInt32()
                : 0;

            return (memberId, workoutSessionId);
        }

        var memberFromQuery = request.Query["memberId"].ToString();
        var workoutFromQuery = int.TryParse(request.Query["workoutSessionId"].ToString(), out var sessionId)
            ? sessionId
            : 0;

        return (memberFromQuery, workoutFromQuery);
    }
}
