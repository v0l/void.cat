using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Controllers.Admin;

[Route("admin")]
[Authorize(Policy = Policies.RequireAdmin)]
public class AdminController : Controller
{
    private readonly IFileStore _fileStore;
    private readonly IFileInfoManager _fileInfo;
    private readonly IUserStore _userStore;

    public AdminController(IFileStore fileStore, IUserStore userStore, IFileInfoManager fileInfo)
    {
        _fileStore = fileStore;
        _userStore = userStore;
        _fileInfo = fileInfo;
    }

    [HttpPost]
    [Route("file")]
    public async Task<RenderedResults<PublicVoidFile>> ListFiles([FromBody] PagedRequest request)
    {
        return await (await _fileStore.ListFiles(request)).GetResults();
    }

    [HttpDelete]
    [Route("file/{id}")]
    public async Task DeleteFile([FromRoute] string id)
    {
        var gid = id.FromBase58Guid();
        await _fileStore.DeleteFile(gid);
        await _fileInfo.Delete(gid);
    }

    [HttpPost]
    [Route("user")]
    public async Task<RenderedResults<PrivateVoidUser>> ListUsers([FromBody] PagedRequest request)
    {
        var result = await _userStore.ListUsers(request);
        return await result.GetResults();
    }
}