namespace Domain.Entities;

public enum ContestStatus
{
    Draft,
    Public,
    Finalized,
}

public class Contest
{
    public int Id { get; init; }
    public required string Name { get; set; }
    public DateTime LockDate { get; set; } = DateTime.UtcNow.AddDays(3);
    public ContestStatus Status { get; set; } = ContestStatus.Draft;
}
