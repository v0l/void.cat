using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoidCat.Model;

namespace VoidCat.Controllers.Admin;

[Route("admin")]
[Authorize(Policy = Policies.RequireAdmin)]
public class AdminController : Controller
{
    
}
