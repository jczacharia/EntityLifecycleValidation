using Application.Persistence;
using Domain.Entities;
using Mediator;

namespace Application.Contests;

public record UpdateContestCommand(int Id, string Name, DateTime LockDate) : ICommand<Contest>;

public class UpdateContestCommandHandler : ICommandHandler<UpdateContestCommand, Contest>
{
    private readonly DbCtx _dbCtx;

    public UpdateContestCommandHandler(DbCtx dbCtx) => _dbCtx = dbCtx;

    public async ValueTask<Contest> Handle(UpdateContestCommand command, CancellationToken cancellationToken)
    {
        Contest contest = await _dbCtx.Contests.FindAsync(command.Id, cancellationToken)
                          ?? throw new Exception("Contest not found.");

        contest.Name = command.Name;
        contest.LockDate = command.LockDate;

        await _dbCtx.SaveChangesAsync(cancellationToken);

        return contest;
    }
}
