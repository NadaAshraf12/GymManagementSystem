using System;

namespace GymManagementSystem.Application.DTOs;

public class CreateTrainerDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Certification { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public string BankAccount { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateTrainerDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Certification { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public string BankAccount { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TrainerReadDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Certification { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public string BankAccount { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TrainerDto
{
    public string? Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Certification { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public string BankAccount { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TrainerAssignmentDetailDto
{
    public int Id { get; set; }
    public string MemberId { get; set; } = string.Empty;
    public string MemberCode { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public string? Notes { get; set; }
}

