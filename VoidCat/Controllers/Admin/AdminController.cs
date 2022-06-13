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
    private readonly IFileMetadataStore _fileMetadata;
    private readonly IFileInfoManager _fileInfo;
    private readonly IUserStore _userStore;
    private readonly IUserUploadsStore _userUploads;

    public AdminController(IFileStore fileStore, IUserStore userStore, IFileInfoManager fileInfo,
        IFileMetadataStore fileMetadata, IUserUploadsStore userUploads)
    {
        _fileStore = fileStore;
        _userStore = userStore;
        _fileInfo = fileInfo;
        _fileMetadata = fileMetadata;
        _userUploads = userUploads;
    }

    /// <summary>
    /// List all files in the system
    /// </summary>
    /// <param name="request">Page request</param>
    /// <returns></returns>
    [HttpPost]
    [Route("file")]
    public async Task<RenderedResults<PublicVoidFile>> ListFiles([FromBody] PagedRequest request)
    {
        var files = await _fileMetadata.ListFiles<VoidFileMeta>(request);

        return new()
        {
            Page = files.Page,
            PageSize = files.PageSize,
            TotalResults = files.TotalResults,
            Results = (await files.Results.SelectAwait(a => _fileInfo.Get(a.Id)).ToListAsync())!
        };
    }

    /// <summary>
    /// Delete a file from the system
    /// </summary>
    /// <param name="id">Id of the file to delete</param>
    [HttpDelete]
    [Route("file/{id}")]
    public async Task DeleteFile([FromRoute] string id)
    {
        var gid = id.FromBase58Guid();
        await _fileStore.DeleteFile(gid);
        await _fileInfo.Delete(gid);
    }

    /// <summary>
    /// List all users in the system
    /// </summary>
    /// <param name="request">Page request</param>
    /// <returns></returns>
    [HttpPost]
    [Route("user")]
    public async Task<RenderedResults<AdminListedUser>> ListUsers([FromBody] PagedRequest request)
    {
        var result = await _userStore.ListUsers(request);

        var ret = await result.Results.SelectAwait(async a =>
        {
            var uploads = await _userUploads.ListFiles(a.Id, new(0, int.MaxValue));
            return new AdminListedUser(a, uploads.TotalResults);
        }).ToListAsync();
        return new()
        {
            PageSize = request.PageSize,
            Page = request.Page,
            TotalResults = result.TotalResults,
            Results = ret
        };
    }

    public record AdminListedUser(PrivateVoidUser User, int Uploads);
}