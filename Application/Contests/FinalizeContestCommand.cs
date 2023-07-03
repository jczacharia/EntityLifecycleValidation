using Application.Persistence;
using Domain.Entities;
using Mediator;

namespace Application.Contests;

public record FinalizeContestCommand(int Id) : ICommand<Contest>;

public class FinalizeContestCommandHandler : ICommandHandler<FinalizeContestCommand, Contest>
{
    private readonly DbCtx _dbCtx;

    public FinalizeContestCommandHandler(DbCtx dbCtx) => _dbCtx = dbCtx;

    public async ValueTask<Contest> Handle(FinalizeContestCommand command, CancellationToken cancellationToken)
    {
        Contest contest = await _dbCtx.Contests.FindAsync(command.Id, cancellationToken)
                          ?? throw new Exception("Contest not found.");

        contest.Status = ContestStatus.Finalized;

        // Calculate the results of the contest here...

        await _dbCtx.SaveChangesAsync(cancellationToken);

        return contest;
    }
}
