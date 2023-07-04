namespace Domain.Entities;

public class Contestant
{
    public int Id { get; init; }
    public required User User { get; set; }
    public required Contest Contest { get; set; }
}
