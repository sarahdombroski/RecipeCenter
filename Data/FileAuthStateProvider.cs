using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace TeamProjectYay.Data;

public class FileAuthStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_currentUser != null)
        {
            return Task.FromResult(new AuthenticationState(_currentUser));
        }

        // anonymous by default
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }


    public void SignIn(AppUser user)
    {
        Console.WriteLine($"Signing in user: {user.Username}");
        var claims = new List<Claim>
        { 
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("FirstName", user.FirstName),
            new Claim("LastName", user.LastName),
            new Claim("ProfilePicturePath", user.ProfilePicturePath)
        };

        foreach (var role in user.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, "FileAuth");
        _currentUser = new ClaimsPrincipal(identity);
        Console.WriteLine($"SignIn: IsAuthenticated={_currentUser.Identity?.IsAuthenticated}, Name={_currentUser.Identity?.Name}");
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void SignOut()
    {
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
