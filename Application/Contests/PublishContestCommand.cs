using Application.Persistence;
using Domain.Entities;
using Mediator;

namespace Application.Contests;

public record PublishContestCommand(int Id) : ICommand<Contest>;

public class PublishContestCommandHandler : ICommandHandler<PublishContestCommand, Contest>
{
    private readonly DbCtx _dbCtx;

    public PublishContestCommandHandler(DbCtx dbCtx) => _dbCtx = dbCtx;

    public async ValueTask<Contest> Handle(PublishContestCommand command, CancellationToken cancellationToken)
    {
        Contest contest = await _dbCtx.Contests.FindAsync(command.Id, cancellationToken)
                          ?? throw new Exception("Contest not found.");

        contest.Status = ContestStatus.Public;
        await _dbCtx.SaveChangesAsync(cancellationToken);

        return contest;
    }
}
