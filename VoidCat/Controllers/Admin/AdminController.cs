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

    [HttpGet]
    [Route("file")]
    public IAsyncEnumerable<PublicVoidFile> ListFiles()
    {
        return _fileStore.ListFiles();
    }

    [HttpDelete]
    [Route("file/{id}")]
    public ValueTask DeleteFile([FromRoute] string id)
    {
        return _fileStore.DeleteFile(id.FromBase58Guid());
    }
}