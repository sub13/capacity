using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;
using Teams.Models;
using Teams.Services;

namespace Teams.Controllers
{
    public class ManageTeamMembersController : Controller
    {
        private readonly IManageTeamsMembersService _teamsMembersService;

        public ManageTeamMembersController(IManageTeamsMembersService teamsMembersService)
        {
            _teamsMembersService = teamsMembersService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        [Authorize]
        public IActionResult Remove()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Remove(int team_id, string member_id)
        {
            var result = await _teamsMembersService.RemoveAsync(team_id, member_id);
            return RedirectToAction("Index");
        }
    }
}