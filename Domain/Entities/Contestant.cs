using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class Contestant
{
    public int Id { get; init; }

    [ForeignKey(nameof(UserId))]
    public required User User { get; set; }

    [ForeignKey(nameof(ContestId))]
    public required Contest Contest { get; set; }

    public int UserId { get; }
    public int ContestId { get; }
}
