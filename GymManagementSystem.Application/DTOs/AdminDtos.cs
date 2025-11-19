using System;
using System.Collections.Generic;

namespace GymManagementSystem.Application.DTOs;

public class LoginAuditDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }
    public DateTime? LogoutTime { get; set; }
    public bool IsSuccessful { get; set; }
    public string? FailureReason { get; set; }
}

public class ActiveSessionDto
{
    public int AuditId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime LoginTime { get; set; }
    public TimeSpan Duration { get; set; }
}

public class UserLookupDto
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Code { get; set; }
}

public class AssignTrainerLookupDto
{
    public IReadOnlyList<UserLookupDto> Trainers { get; init; } = Array.Empty<UserLookupDto>();
    public IReadOnlyList<UserLookupDto> Members { get; init; } = Array.Empty<UserLookupDto>();
}

public class PaginatedResult<T>
{
    public PaginatedResult(IReadOnlyList<T> items, int totalItems, int currentPage, int pageSize)
    {
        Items = items;
        TotalItems = totalItems;
        CurrentPage = currentPage;
        PageSize = pageSize;
        TotalPages = pageSize == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);
    }

    public IReadOnlyList<T> Items { get; }
    public int TotalItems { get; }
    public int CurrentPage { get; }
    public int PageSize { get; }
    public int TotalPages { get; }
}

