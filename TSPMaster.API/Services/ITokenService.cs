using TSPMaster.API.Models;

namespace TSPMaster.API.Services;

public interface ITokenService
{
    string GenerateToken(ApplicationUser user, IList<string> roles);
}
