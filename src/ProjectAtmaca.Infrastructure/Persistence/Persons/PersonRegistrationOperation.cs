namespace ProjectAtmaca.Infrastructure.Persistence.Persons;

public sealed class PersonRegistrationOperation
{
    public Guid ActorId { get; set; }
    public Guid OperationId { get; set; }
    public string InputJson { get; set; } = null!;
    public Guid PersonId { get; set; }
    public Guid CardId { get; set; }
    public string CardNumber { get; set; } = null!;
    public DateTime IssuedAtUtc { get; set; }
}
