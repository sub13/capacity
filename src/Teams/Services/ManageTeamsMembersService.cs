using Teams.Models;
using Teams.Data;
using System.Linq;
using Teams.Services;
using Teams.Security;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Teams.Services
{
    public class ManageTeamsMembersService : IManageTeamsMembersService
    {
        private readonly ICurrentUser _currentUser;
        private readonly IRepository<Team, int> _teamRepository;
        private readonly IRepository<TeamMember, int> _memberRepository;

        public ManageTeamsMembersService(IRepository<Team, int> teamRepository, IRepository<TeamMember, int> memberRepository, ICurrentUser currentUser)
        {
            _currentUser = currentUser;
            _teamRepository = teamRepository;
            _memberRepository = memberRepository;
        }

        public async Task<bool> AddAsync(int team_id, string member_id)
        {
            Team team = await _teamRepository.GetByIdAsync(team_id);
            if (team != null && team.TeamOwner == _currentUser.Current.Id() && !AlreadyInTeam(team_id, member_id))
            {
                TeamMember member = new TeamMember { TeamId = team_id, MemberId = member_id };
                return await _memberRepository.InsertAsync(member);
            }
            return false;
        }

        private bool AlreadyInTeam(int team_id, string member_id)
        {
            return _memberRepository.GetAll().Any(t => t.TeamId == team_id && t.MemberId == member_id);
        }
    }
}