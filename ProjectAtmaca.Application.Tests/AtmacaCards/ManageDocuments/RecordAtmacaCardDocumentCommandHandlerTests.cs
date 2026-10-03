using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectAtmaca.Application.Abstractions.Files;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.AtmacaCards.ManageDocuments;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Application.Tests.AtmacaCards.ManageDocuments;

public sealed class RecordAtmacaCardDocumentCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldStoreAndRecordValidDocument()
    {
        var card = CreateCard();
        var storage = new FakeStorage();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(card, storage, unitOfWork);

        var result = await handler.Handle(CreateCommand(card), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Antrenörlük Sertifikası");
        storage.StoredFiles.Should().ContainSingle();
        storage.DeletedKeys.Should().BeEmpty();
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldRejectContentThatDoesNotMatchFileExtension()
    {
        var card = CreateCard();
        var storage = new FakeStorage();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(card, storage, unitOfWork);
        var command = CreateCommand(card, [0x01, 0x02, 0x03]);

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ATMACA_CARD_DOCUMENT_FILE_CONTENT_INVALID");
        storage.StoredFiles.Should().BeEmpty();
        unitOfWork.SaveCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldDeleteStoredFileWhenDatabaseSaveFails()
    {
        var card = CreateCard();
        var storage = new FakeStorage();
        var unitOfWork = new FakeUnitOfWork { ShouldFail = true };
        var handler = CreateHandler(card, storage, unitOfWork);

        var act = () => handler.Handle(CreateCommand(card), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
        storage.DeletedKeys.Should().ContainSingle()
            .Which.Should().StartWith($"atmaca-cards/{card.AtmacaCardId.Value:N}/");
        storage.StoredFiles.Should().BeEmpty();
    }

    private static RecordAtmacaCardDocumentCommandHandler CreateHandler(
        AtmacaCard card,
        FakeStorage storage,
        FakeUnitOfWork unitOfWork) =>
        new(
            new AllowingAuthorization(),
            new FakeRepository(card),
            storage,
            unitOfWork,
            NullLogger<RecordAtmacaCardDocumentCommandHandler>.Instance);

    private static RecordAtmacaCardDocumentCommand CreateCommand(
        AtmacaCard card,
        byte[]? content = null)
    {
        byte[] bytes = content ?? "%PDF-1.7 test"u8.ToArray();
        return new RecordAtmacaCardDocumentCommand(
            card.AtmacaCardId,
            AtmacaCardDocumentType.Certificate,
            "Antrenörlük Sertifikası",
            "Federasyon",
            new DateOnly(2025, 5, 10),
            "certificate.pdf",
            "application/pdf",
            bytes.Length,
            new MemoryStream(bytes));
    }

    private static AtmacaCard CreateCard() =>
        AtmacaCard.Issue(
            Guid.NewGuid(),
            AtmacaCardNumber.Create("ATM-000090").Value!,
            DateTime.UtcNow).Value!;

    private sealed class FakeRepository(AtmacaCard card) : IAtmacaCardRepository
    {
        public Task<AtmacaCard?> GetByIdAsync(
            AtmacaCardId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AtmacaCard?>(card.AtmacaCardId == id ? card : null);
    }

    private sealed class FakeStorage : IAtmacaCardDocumentStorage
    {
        public Dictionary<string, byte[]> StoredFiles { get; } = [];
        public List<string> DeletedKeys { get; } = [];

        public async Task StoreAsync(
            string storageKey,
            Stream content,
            CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            StoredFiles.Add(storageKey, buffer.ToArray());
        }

        public Task<Stream> OpenReadAsync(
            string storageKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream(StoredFiles[storageKey], writable: false));

        public Task DeleteAsync(
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            DeletedKeys.Add(storageKey);
            StoredFiles.Remove(storageKey);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }
        public bool ShouldFail { get; init; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return ShouldFail
                ? Task.FromException<int>(new InvalidOperationException("Save failed."))
                : Task.FromResult(1);
        }
    }

    private sealed class AllowingAuthorization : IActorAuthorizationService
    {
        public Task<Result> AuthorizeAsync(
            Permission permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }
}
