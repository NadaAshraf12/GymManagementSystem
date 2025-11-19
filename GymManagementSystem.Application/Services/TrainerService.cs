using System.Linq;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Application.Services;

public class TrainerService : ITrainerService
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private const string TrainerRole = "Trainer";
    private const string DefaultPassword = "Gym@12345";

    public TrainerService(
        IApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IReadOnlyList<TrainerDto>> GetAllAsync()
    {
        var trainers = await _context.Trainers
            .AsNoTracking()
            .OrderBy(t => t.FirstName)
            .ThenBy(t => t.LastName)
            .ToListAsync();

        return trainers.Select(MapToDto).ToList();
    }

    public async Task<TrainerDto?> GetByIdAsync(string id)
    {
        var trainer = await _context.Trainers.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        return trainer == null ? null : MapToDto(trainer);
    }

    public async Task<OperationResultDto> CreateAsync(TrainerDto dto)
    {
        if (!await _roleManager.RoleExistsAsync(TrainerRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(TrainerRole));
        }

        var trainer = new Trainer
        {
            Id = Guid.NewGuid().ToString(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            UserName = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Specialty = dto.Specialty,
            Certification = dto.Certification,
            Experience = dto.Experience,
            Salary = dto.Salary,
            BankAccount = dto.BankAccount,
            HireDate = DateTime.UtcNow,
            IsActive = dto.IsActive,
            MustChangePassword = true
        };

        var createResult = await _userManager.CreateAsync(trainer, DefaultPassword);
        if (!createResult.Succeeded)
        {
            var message = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return OperationResultDto.Fail(message);
        }

        await _userManager.AddToRoleAsync(trainer, TrainerRole);
        return OperationResultDto.Ok("Trainer created successfully.");
    }

    public async Task<OperationResultDto> UpdateAsync(TrainerDto dto)
    {
        if (string.IsNullOrEmpty(dto.Id))
            return OperationResultDto.Fail("Trainer id is required.");

        var trainer = await _userManager.FindByIdAsync(dto.Id) as Trainer;
        if (trainer == null)
            return OperationResultDto.Fail("Trainer not found.");

        trainer.FirstName = dto.FirstName;
        trainer.LastName = dto.LastName;
        trainer.Email = dto.Email;
        trainer.UserName = dto.Email;
        trainer.PhoneNumber = dto.PhoneNumber;
        trainer.Specialty = dto.Specialty;
        trainer.Certification = dto.Certification;
        trainer.Experience = dto.Experience;
        trainer.Salary = dto.Salary;
        trainer.BankAccount = dto.BankAccount;
        trainer.IsActive = dto.IsActive;

        var updateResult = await _userManager.UpdateAsync(trainer);
        if (!updateResult.Succeeded)
        {
            var message = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return OperationResultDto.Fail(message);
        }

        return OperationResultDto.Ok("Trainer updated successfully.");
    }

    public async Task<OperationResultDto> DeleteAsync(string id)
    {
        var trainer = await _userManager.FindByIdAsync(id) as Trainer;
        if (trainer == null)
            return OperationResultDto.Fail("Trainer not found.");

        var hasAssignments = await _context.TrainerMemberAssignments.AnyAsync(a => a.TrainerId == id);
        if (hasAssignments)
            return OperationResultDto.Fail("Trainer has active assignments. Remove members first.");

        var deleteResult = await _userManager.DeleteAsync(trainer);
        if (!deleteResult.Succeeded)
        {
            var message = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
            return OperationResultDto.Fail(message);
        }

        return OperationResultDto.Ok("Trainer deleted successfully.");
    }

    private static TrainerDto MapToDto(Trainer trainer) => new()
    {
        Id = trainer.Id,
        FirstName = trainer.FirstName,
        LastName = trainer.LastName,
        Email = trainer.Email ?? string.Empty,
        PhoneNumber = trainer.PhoneNumber ?? string.Empty,
        Specialty = trainer.Specialty,
        Certification = trainer.Certification,
        Experience = trainer.Experience,
        Salary = trainer.Salary,
        BankAccount = trainer.BankAccount,
        IsActive = trainer.IsActive
    };
}

