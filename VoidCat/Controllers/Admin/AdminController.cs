using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VoidCat.Controllers.Admin;

[Route("admin")]
[Authorize(Policy = "Admin")]
public class AdminController : Controller
{
    
}
