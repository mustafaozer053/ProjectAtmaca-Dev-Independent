using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectAtmaca.Application.Abstractions.Files;
using ProjectAtmaca.Application.Abstractions.Persistence;
using ProjectAtmaca.Application.Abstractions.Security;
using ProjectAtmaca.Application.AtmacaCards.ManagePhoto;
using ProjectAtmaca.Domain.Actors;
using ProjectAtmaca.Domain.AtmacaCards;
using ProjectAtmaca.Domain.Common;
using ProjectAtmaca.Domain.Common.ValueObjects;

namespace ProjectAtmaca.Application.Tests.AtmacaCards.ManagePhoto;

public sealed class SaveAtmacaCardPhotoCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldStoreAndSaveValidPngPhoto()
    {
        var card = CreateCard();
        var storage = new FakeStorage();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(card, storage, unitOfWork);

        var result = await handler.Handle(
            CreateCommand(card, PngSignature),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        card.PhotoContentType.Should().Be("image/png");
        card.PhotoStorageKey.Should().StartWith($"atmaca-cards/{card.AtmacaCardId.Value:N}/photo-");
        storage.StoredFiles.Should().ContainKey(card.PhotoStorageKey!);
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldRejectInvalidImageContentBeforeStoring()
    {
        var card = CreateCard();
        var storage = new FakeStorage();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(card, storage, unitOfWork);

        var result = await handler.Handle(
            CreateCommand(card, [0x01, 0x02, 0x03]),
            TestContext.Current.CancellationToken);

        result.Error!.Code.Should().Be("ATMACA_CARD_PHOTO_FILE_INVALID");
        storage.StoredFiles.Should().BeEmpty();
        unitOfWork.SaveCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldDeleteOldPhotoAfterReplacement()
    {
        var card = CreateCard();
        var storage = new FakeStorage();
        var handler = CreateHandler(card, storage, new FakeUnitOfWork());

        (await handler.Handle(CreateCommand(card, PngSignature), TestContext.Current.CancellationToken))
            .IsSuccess.Should().BeTrue();
        string oldKey = card.PhotoStorageKey!;

        (await handler.Handle(CreateCommand(card, JpegSignature, "photo.jpg", "image/jpeg"),
            TestContext.Current.CancellationToken)).IsSuccess.Should().BeTrue();

        card.PhotoStorageKey.Should().NotBe(oldKey);
        storage.StoredFiles.Should().ContainKey(card.PhotoStorageKey!);
        storage.StoredFiles.Should().NotContainKey(oldKey);
    }

    private static SaveAtmacaCardPhotoCommandHandler CreateHandler(
        AtmacaCard card,
        FakeStorage storage,
        FakeUnitOfWork unitOfWork) =>
        new(
            new AllowingAuthorization(),
            new FakeRepository(card),
            storage,
            unitOfWork,
            NullLogger<SaveAtmacaCardPhotoCommandHandler>.Instance);

    private static SaveAtmacaCardPhotoCommand CreateCommand(
        AtmacaCard card,
        byte[] content,
        string fileName = "photo.png",
        string contentType = "image/png") =>
        new(
            card.AtmacaCardId,
            fileName,
            contentType,
            content.Length,
            new MemoryStream(content));

    private static AtmacaCard CreateCard() =>
        AtmacaCard.Issue(
            Guid.NewGuid(),
            AtmacaCardNumber.Create("ATM-000091").Value!,
            DateTime.UtcNow).Value!;

    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF, 0xD9];

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
            StoredFiles.Remove(storageKey);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
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
