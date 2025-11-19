using System.Collections.Generic;
using System.Linq;
using GymManagementSystem.Application.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GymManagementSystem.WebUI.Models.Admin;

public class AssignTrainerViewModel
{
    public AssignTrainerDto Input { get; set; } = new();
    public IEnumerable<SelectListItem> Trainers { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Members { get; set; } = Enumerable.Empty<SelectListItem>();
}

