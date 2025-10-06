using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PlansController : ControllerBase
{
    private readonly ITrainingPlanService _trainingPlanService;
    private readonly INutritionPlanService _nutritionPlanService;
    public PlansController(ITrainingPlanService trainingPlanService, INutritionPlanService nutritionPlanService)
    {
        _trainingPlanService = trainingPlanService;
        _nutritionPlanService = nutritionPlanService;
    }

    [HttpPost("training")]
    [Authorize(Roles = "Trainer,Admin")]
    public async Task<ActionResult<int>> CreateTrainingPlan(CreateTrainingPlanDto dto)
    {
        var id = await _trainingPlanService.CreateAsync(dto);
        return Ok(id);
    }

    [HttpPost("nutrition")]
    [Authorize(Roles = "Trainer,Admin")]
    public async Task<ActionResult<int>> CreateNutritionPlan(CreateNutritionPlanDto dto)
    {
        var id = await _nutritionPlanService.CreateAsync(dto);
        return Ok(id);
    }
}


