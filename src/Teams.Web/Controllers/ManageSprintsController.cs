﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Primitives;
using Teams.Business.Services;
using Teams.Data.Models;
using Teams.Web.ViewModels;
using Teams.Web.ViewModels.Sprint;
using Teams.Web.ViewModels.Task;
using Teams.Web.ViewModels.Team;
using Teams.Web.ViewModels.TeamMember;

namespace Teams.Web.Controllers
{
    public class ManageSprintsController : Controller
    {
        private readonly IManageSprintsService _manageSprintsService;
        private readonly IAccessCheckService _accessCheckService;
        private readonly IManageTeamsService _manageTeamsService;
        private readonly IManageTeamsMembersService _manageTeamsMembersService;
        private readonly IManageTasksService _manageTasksService;
        private readonly IStringLocalizer<ManageSprintsController> _localizer;


        public ManageSprintsController(IManageSprintsService manageSprintsService, IAccessCheckService accessCheckService, 
            IManageTeamsService manageTeamsService, IManageTeamsMembersService manageTeamsMembersService, 
            IManageTasksService manageTasksService, IStringLocalizer<ManageSprintsController> localizer)
        {
            _manageSprintsService = manageSprintsService;
            _manageTeamsService = manageTeamsService;
            _accessCheckService = accessCheckService;
            _manageTeamsMembersService = manageTeamsMembersService;
            _manageTasksService = manageTasksService;
            _localizer = localizer;
        }

        [Authorize]
        public async Task<IActionResult> AllSprints(int teamId, DisplayOptions options)
        {
            List<Sprint> sprints;
            if (await _accessCheckService.OwnerOrMemberAsync(teamId))
            {
                sprints = (List<Sprint>)await _manageSprintsService.GetAllSprintsAsync(teamId, options);
            }
            else 
                return View("ErrorGetAllSprints");

            var team = await _manageSprintsService.GetTeam(teamId);

            if (await _accessCheckService.IsOwnerAsync(teamId)) ViewBag.AddVision = "visible";
            else ViewBag.AddVision = "collapse";

            ViewData["DaysInSprint"] = _localizer["DaysInSprint"];
            ViewData["StoryPointInHours"] = _localizer["StoryPointInHours"];
            ViewData["NameOfSprint"] = _localizer["NameOfSprint"];
            ViewData["AddMember"] = _localizer["AddMember"];
            ViewData["RemoveMember"] = _localizer["RemoveMember"];
            ViewData["Remove"] = _localizer["Remove"];
            ViewData["Cancel"] = _localizer["Cancel"];

            List<TeamMember> teamMembers = await GetAllTeamMembersAsync(teamId, new DisplayOptions { });
            var sprintAndTeam = new SprintAndTeamViewModel
            {
                Sprints = new List<SprintViewModel>()
            };
            sprints.ForEach(t=>sprintAndTeam.Sprints.Add(new SprintViewModel()
                {
                    Id = t.Id,
                    DaysInSprint =t.DaysInSprint,
                    Status = t.Status,
                    Name = t.Name,
                    StoryPointInHours = t.StoryPointInHours,
                    TeamId = t.TeamId
                }
            ));
            sprintAndTeam.Team = new TeamViewModel(){Id = team.Id,Owner = team.Owner,TeamName = team.TeamName,TeamMembers = new List<TeamMemberViewModel>()};
            teamMembers.ForEach(t=>sprintAndTeam.Team.TeamMembers.Add(new TeamMemberViewModel(){Member = t.Member,MemberId = t.MemberId}));
            return View(sprintAndTeam);
        }

        [Authorize, NonAction]
        private async Task<List<TeamMember>> GetAllTeamMembersAsync(int teamId, DisplayOptions options)
        {
            if (!await _accessCheckService.OwnerOrMemberAsync(teamId))
            {
                RedirectToAction("Error");
            }
            return await _manageTeamsMembersService.GetAllTeamMembersAsync(teamId, options);
        }

        [Authorize]
        public async Task<IActionResult> CompleteTaskInSprint(int taskId, bool isCompleted)
        {
            var task = await _manageTasksService.GetTaskByIdAsync(taskId);
            var sprint = await _manageSprintsService.GetSprintAsync(task.SprintId, false);

            if (sprint != null && sprint.Status == PossibleStatuses.ActiveStatus)
            {
                task.Completed = isCompleted;
            }
            else
            {
                return View("Error");
            }

            var result = await _manageTasksService.EditTaskAsync(task);

            if (result)
            {
                return RedirectToAction("GetSprintById", new { sprintId = task.SprintId });
            }
            else
            {
                task.Completed = false;
                return View("Error");
            }
        }

        [Authorize]
        public async Task<IActionResult> GetSprintById(int sprintId)
        {
            var sprint = await _manageSprintsService.GetSprintAsync(sprintId, true);

            if (sprint == null)
                return View("ErrorGetAllSprints");

            var sprintViewModel = new SprintViewModel()
            {
                DaysInSprint = sprint.DaysInSprint,
                Id = sprint.Id,
                Tasks = new List<TaskViewModel>(),
                Status = sprint.Status,
                Name = sprint.Name,
                StoryPointInHours = sprint.StoryPointInHours,
                TeamId = sprint.TeamId
            };

            sprint.Tasks.ToList().ForEach(t=>sprintViewModel.Tasks.Add(new TaskViewModel()
                {
                    TeamMember = new TeamMemberViewModel(){Member = t.TeamMember.Member, MemberId = t.MemberId.ToString()},
                    Name = t.Name,
                    StoryPoints = t.StoryPoints,
                    Id = t.Id,
                    Link = t.Link,
                    Completed = t.Completed
                }
            ));

            if (await _accessCheckService.IsOwnerAsync(sprint.TeamId)) ViewBag.AddVision = "visible";
            else ViewBag.AddVision = "collapse";

            
            return View(sprintViewModel);
        }

        [Authorize]
        public async Task<IActionResult> EditSprintAsync(int teamId, int sprintId, string errorMessage)
        {
            var team = await _manageSprintsService.GetTeam(teamId);
            var sprint = await _manageSprintsService.GetSprintAsync(sprintId,false);

            EditSprintViewModel model = new EditSprintViewModel 
            {
                TeamId = teamId,
                TeamName = team.TeamName,
                SprintId = sprint.Id,
                SprintName = sprint.Name,
                SprintDaysInSprint = sprint.DaysInSprint,
                SprintStorePointInHours = sprint.StoryPointInHours,
                ErrorMessage = errorMessage
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EditSprintAsync(EditSprintViewModel editSprintViewModel)
        {
            if (ModelState.IsValid)
            {
                var sprints = await _manageSprintsService.GetAllSprintsAsync(editSprintViewModel.TeamId, new DisplayOptions());

                var sprint = new Sprint
                {
                    Id = editSprintViewModel.SprintId,
                    TeamId = editSprintViewModel.TeamId,
                    Name = editSprintViewModel.SprintName,
                    DaysInSprint = editSprintViewModel.SprintDaysInSprint,
                    StoryPointInHours = editSprintViewModel.SprintStorePointInHours
                };

                var result = await EditSprintAsync(sprint);

                if (result)
                {
                    return RedirectToAction("AllSprints", new { teamId = editSprintViewModel.TeamId });
                }
                else
                {
                    return RedirectToAction("NotOwnerError", new { teamId = editSprintViewModel.TeamId });
                }
            }

            return View(editSprintViewModel);
        }

        [Authorize]
        public async Task<IActionResult> AddSprintAsync(int teamId, string errorMessage)
        {
            var team = await _manageSprintsService.GetTeam(teamId);
            var sprintViewModel = new SprintViewModel() { Id = teamId, Name = team.TeamName };
            ViewBag.ErrorMessage = errorMessage;

            return View(sprintViewModel);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddSprintAsync(SprintViewModel sprintViewModel)
        {
            if (ModelState.IsValid)
            {
                var sprints = await _manageSprintsService.GetAllSprintsAsync(sprintViewModel.TeamId, new DisplayOptions());

                var sprint = new Sprint
                {
                    TeamId = sprintViewModel.TeamId,
                    Name = sprintViewModel.Name,
                    DaysInSprint = sprintViewModel.DaysInSprint,
                    StoryPointInHours = sprintViewModel.StoryPointInHours
                };

                var result = await AddSprintAsync(sprint);

                if (result)
                {
                    return RedirectToAction("AllSprints", new { teamId = sprintViewModel.TeamId });
                }
                else
                {
                    return RedirectToAction("NotOwnerError", new { teamId = sprintViewModel.TeamId });
                }
            }

            return View(sprintViewModel);
        }

        public IActionResult NotOwnerError(int teamId)
        {
            ViewData["Error"] = _localizer["Error"];
            ViewData["Cause"] = _localizer["NotOwner"];
            return View(teamId);
        }

        public IActionResult Error()
        {
            ViewData["Error"] = _localizer["Error"];
            return View();
        }

        [Authorize, NonAction]
        private async Task<bool> AddSprintAsync(Sprint sprint)
        {
            if (await _accessCheckService.IsOwnerAsync(sprint.TeamId))
            {
                return await _manageSprintsService.AddSprintAsync(sprint);
            }
            else return false;
        }

        [Authorize, NonAction]
        private async Task<bool> EditSprintAsync(Sprint sprint)
        {
            if (await _accessCheckService.IsOwnerAsync(sprint.TeamId))
            {
                return await _manageSprintsService.EditSprintAsync(sprint);
            }
            else return false;
        }
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Remove(int sprintId)
        {
            var sprint = await _manageSprintsService.GetSprintAsync(sprintId,false);
            var teamId = sprint.TeamId;
            var result = await _manageSprintsService.RemoveAsync(sprintId);
            if (result)
                return RedirectToAction("AllSprints",new { teamId = teamId});
            return RedirectToAction("ErrorRemove");
        }

        public IActionResult ErrorRemove()
        {
            return View();
        }
    }
}
