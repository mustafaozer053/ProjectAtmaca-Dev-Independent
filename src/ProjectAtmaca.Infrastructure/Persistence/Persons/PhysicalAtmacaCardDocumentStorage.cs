using ProjectAtmaca.Application.Abstractions.Files;
using Microsoft.Extensions.Configuration;

namespace ProjectAtmaca.Infrastructure.Persistence.Persons;

public sealed class PhysicalAtmacaCardDocumentStorage(IConfiguration configuration)
    : IAtmacaCardDocumentStorage
{
    public async Task StoreAsync(
        string storageKey,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        string path = ResolvePath(storageKey);
        string? directory = Path.GetDirectoryName(path);
        if (directory is null)
            throw new InvalidOperationException("The configured document storage path is invalid.");

        Directory.CreateDirectory(directory);
        await using var target = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);
        await content.CopyToAsync(target, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Stream stream = new FileStream(
            ResolvePath(storageKey),
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string path = ResolvePath(storageKey);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) ||
            Path.IsPathRooted(storageKey) ||
            storageKey.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("Storage key is invalid.", nameof(storageKey));
        }

        string rootPath = configuration["FileStorage:RootPath"]?.Trim() ?? string.Empty;
        if (rootPath.Length == 0)
            throw new InvalidOperationException(
                "FileStorage:RootPath must be configured to an attached disk or NAS share before document files can be stored.");

        string fullRoot = Path.GetFullPath(rootPath);
        string fullPath = Path.GetFullPath(Path.Combine(fullRoot, storageKey));
        string rootWithSeparator = fullRoot.EndsWith(Path.DirectorySeparatorChar)
            ? fullRoot
            : fullRoot + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Storage key resolves outside the configured root.", nameof(storageKey));

        return fullPath;
    }
}
