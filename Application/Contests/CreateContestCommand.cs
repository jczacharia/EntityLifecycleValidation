using Application.Persistence;
using Domain.Entities;
using Mediator;

namespace Application.Contests;

public record CreateContestCommand(string Name) : ICommand<Contest>;

public class CreateContestCommandHandler : ICommandHandler<CreateContestCommand, Contest>
{
    private readonly DbCtx _dbCtx;

    public CreateContestCommandHandler(DbCtx dbCtx) => _dbCtx = dbCtx;

    public async ValueTask<Contest> Handle(CreateContestCommand command, CancellationToken cancellationToken)
    {
        Contest contest = new Contest
        {
            Name = command.Name,
        };

        await _dbCtx.Contests.AddAsync(contest, cancellationToken);
        await _dbCtx.SaveChangesAsync(cancellationToken);

        return contest;
    }
}







