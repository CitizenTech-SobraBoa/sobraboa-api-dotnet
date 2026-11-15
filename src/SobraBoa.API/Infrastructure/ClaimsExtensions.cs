using System.Security.Claims;

namespace SobraBoa.API.Infrastructure;

public static class ClaimsExtensions
{
    public static int GetUserId(this ClaimsPrincipal u) => int.Parse(u.FindFirstValue("sub")!);
    public static bool IsAdmin(this ClaimsPrincipal u) => u.IsInRole("Admin");
}