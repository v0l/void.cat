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

    public AdminController(IFileStore fileStore)
    {
        _fileStore = fileStore;
    }

    [HttpPost]
    [Route("file")]
    public Task<RenderedResults<PublicVoidFile>> ListFiles([FromBody] PagedRequest request)
    {
        return _fileStore.ListFiles(request).GetResults();
    }

    [HttpDelete]
    [Route("file/{id}")]
    public ValueTask DeleteFile([FromRoute] string id)
    {
        return _fileStore.DeleteFile(id.FromBase58Guid());
    }
}