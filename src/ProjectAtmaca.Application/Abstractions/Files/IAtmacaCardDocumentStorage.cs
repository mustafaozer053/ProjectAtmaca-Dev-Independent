namespace ProjectAtmaca.Application.Abstractions.Files;

public interface IAtmacaCardDocumentStorage
{
    Task StoreAsync(string storageKey, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
