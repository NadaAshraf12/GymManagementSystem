using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace GymManagementSystem.WebUI.Controllers;

[Authorize(Policy = "TrainerOwnsResource")]
public class PlansController : BaseApiController
{
    private readonly ITrainingPlanService _trainingPlanService;
    private readonly INutritionPlanService _nutritionPlanService;
    public PlansController(ITrainingPlanService trainingPlanService, INutritionPlanService nutritionPlanService)
    {
        _trainingPlanService = trainingPlanService;
        _nutritionPlanService = nutritionPlanService;
    }

    [HttpPost("training")]
    public async Task<ActionResult<ApiResponse<int>>> CreateTrainingPlan(CreateTrainingPlanDto dto)
    {
        var id = await _trainingPlanService.CreateAsync(dto);
        return ApiCreated(id, "Training plan created successfully.");
    }

    [HttpPost("nutrition")]
    public async Task<ActionResult<ApiResponse<int>>> CreateNutritionPlan(CreateNutritionPlanDto dto)
    {
        var id = await _nutritionPlanService.CreateAsync(dto);
        return ApiCreated(id, "Nutrition plan created successfully.");
    }

    [HttpPut("training/{id:int}")]
    public async Task<ActionResult<ApiResponse<TrainingPlanDto>>> UpdateTrainingPlan(int id, UpdateTrainingPlanDto dto)
    {
        dto.Id = id;
        var updated = await _trainingPlanService.UpdateAsync(dto);
        return ApiOk(updated, "Training plan updated successfully.");
    }

    [HttpGet("training/{id:int}")]
    public async Task<ActionResult<ApiResponse<TrainingPlanDto>>> GetTrainingPlanById(int id)
    {
        var plan = await _trainingPlanService.GetByIdAsync(id);
        return ApiOk(plan, "Training plan retrieved successfully.");
    }

    [HttpPut("training/items/{id:int}")]
    public async Task<ActionResult<ApiResponse<TrainingPlanItemDto>>> UpdateTrainingPlanItem(int id, UpdateTrainingPlanItemDto dto)
    {
        dto.Id = id;
        var updated = await _trainingPlanService.UpdateItemAsync(dto);
        return ApiOk(updated, "Training plan item updated successfully.");
    }

    [HttpPut("nutrition/{id:int}")]
    public async Task<ActionResult<ApiResponse<NutritionPlanDto>>> UpdateNutritionPlan(int id, UpdateNutritionPlanDto dto)
    {
        dto.Id = id;
        var updated = await _nutritionPlanService.UpdateAsync(dto);
        return ApiOk(updated, "Nutrition plan updated successfully.");
    }

    [HttpGet("nutrition/{id:int}")]
    public async Task<ActionResult<ApiResponse<NutritionPlanDto>>> GetNutritionPlanById(int id)
    {
        var plan = await _nutritionPlanService.GetByIdAsync(id);
        return ApiOk(plan, "Nutrition plan retrieved successfully.");
    }

    [HttpPut("nutrition/items/{id:int}")]
    public async Task<ActionResult<ApiResponse<NutritionPlanItemDto>>> UpdateNutritionPlanItem(int id, UpdateNutritionPlanItemDto dto)
    {
        dto.Id = id;
        var updated = await _nutritionPlanService.UpdateItemAsync(dto);
        return ApiOk(updated, "Nutrition plan item updated successfully.");
    }
}


