﻿using System.Security.Cryptography;
using VoidCat.Model;
using VoidCat.Services.Abstractions;

namespace VoidCat.Services.VirusScanner.VirusTotal;

public class VirusTotalScanner : IVirusScanner
{
    private readonly ILogger<VirusTotalScanner> _logger;
    private readonly VirusTotalClient _client;
    private readonly IFileStore _fileStore;

    public VirusTotalScanner(ILogger<VirusTotalScanner> logger, VirusTotalClient client, IFileStore fileStore)
    {
        _logger = logger;
        _client = client;
        _fileStore = fileStore;
    }

    public async ValueTask<VirusScanResult> ScanFile(Guid id, CancellationToken cts)
    {
        await using var fs = await _fileStore.Open(new(id, Enumerable.Empty<RangeRequest>()), cts);

        // hash file and check on VT
        var hash = await SHA256.Create().ComputeHashAsync(fs, cts);

        var report = await _client.GetReport(hash.ToHex());
        if (report != default)
        {
            return new()
            {
                IsVirus = report.Attributes.Reputation == 0
            };
        }

        throw new InvalidOperationException();
    }
}