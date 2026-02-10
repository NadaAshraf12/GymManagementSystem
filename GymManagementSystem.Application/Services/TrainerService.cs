using System.Linq;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Application.Services;

public class TrainerService : ITrainerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private const string TrainerRole = "Trainer";
    private const string DefaultPassword = "Gym@12345";

    public TrainerService(
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IReadOnlyList<TrainerReadDto>> GetAllAsync()
    {
        var trainerRepo = _unitOfWork.Repository<Trainer>();
        var trainers = await trainerRepo.ToListAsync(
            trainerRepo.Query()
                .AsNoTracking()
                .OrderBy(t => t.FirstName)
                .ThenBy(t => t.LastName));

        return trainers.Adapt<List<TrainerReadDto>>();
    }

    public async Task<UpdateTrainerDto?> GetByIdAsync(string id)
    {
        var trainerRepo = _unitOfWork.Repository<Trainer>();
        var trainer = await trainerRepo.FirstOrDefaultAsync(
            trainerRepo.Query().AsNoTracking().Where(t => t.Id == id));
        return trainer == null ? null : trainer.Adapt<UpdateTrainerDto>();
    }

    public async Task<OperationResultDto> CreateAsync(CreateTrainerDto dto)
    {
        if (!await _roleManager.RoleExistsAsync(TrainerRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(TrainerRole));
        }

        var trainer = dto.Adapt<Trainer>();
        trainer.Id = Guid.NewGuid().ToString();
        trainer.Email = dto.Email;
        trainer.UserName = dto.Email;
        trainer.PhoneNumber = dto.PhoneNumber;
        trainer.HireDate = DateTime.UtcNow;
        trainer.IsActive = dto.IsActive;
        trainer.MustChangePassword = true;

        var createResult = await _userManager.CreateAsync(trainer, DefaultPassword);
        if (!createResult.Succeeded)
        {
            var message = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return OperationResultDto.Fail(message);
        }

        await _userManager.AddToRoleAsync(trainer, TrainerRole);
        return OperationResultDto.Ok("Trainer created successfully.");
    }

    public async Task<OperationResultDto> UpdateAsync(UpdateTrainerDto dto)
    {
        if (string.IsNullOrEmpty(dto.Id))
            return OperationResultDto.Fail("Trainer id is required.");

        var trainer = await _userManager.FindByIdAsync(dto.Id) as Trainer;
        if (trainer == null)
            return OperationResultDto.Fail("Trainer not found.");

        dto.Adapt(trainer);
        trainer.Email = dto.Email;
        trainer.UserName = dto.Email;
        trainer.PhoneNumber = dto.PhoneNumber;
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

        var assignmentRepo = _unitOfWork.Repository<TrainerMemberAssignment>();
        var hasAssignments = await assignmentRepo.AnyAsync(a => a.TrainerId == id);
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
}

