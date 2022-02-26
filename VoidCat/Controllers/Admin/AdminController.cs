using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Controllers.Admin;

[EnableCors(CorsPolicy.Auth)]
[Route("admin")]
[Authorize(Policy = Policies.RequireAdmin)]
public class AdminController : Controller
{
    private readonly IFileStore _fileStore;
    private readonly IUserStore _userStore;

    public AdminController(IFileStore fileStore, IUserStore userStore)
    {
        _fileStore = fileStore;
        _userStore = userStore;
    }

    [HttpPost]
    [Route("file")]
    public async Task<RenderedResults<PublicVoidFile>> ListFiles([FromBody] PagedRequest request)
    {
        return await (await _fileStore.ListFiles(request)).GetResults();
    }

    [HttpDelete]
    [Route("file/{id}")]
    public ValueTask DeleteFile([FromRoute] string id)
    {
        return _fileStore.DeleteFile(id.FromBase58Guid());
    }

    [HttpPost]
    [Route("user")]
    public async Task<RenderedResults<PublicVoidUser>> ListUsers([FromBody] PagedRequest request)
    {
        var result = await _userStore.ListUsers(request);
        return await result.GetResults();
    }
}
