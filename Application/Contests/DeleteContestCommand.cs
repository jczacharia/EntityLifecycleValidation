using Application.Persistence;
using Domain.Entities;
using Mediator;

namespace Application.Contests;

public record DeleteContestCommand(int Id) : ICommand;

public class DeleteContestCommandHandler : ICommandHandler<DeleteContestCommand>
{
    private readonly DbCtx _dbCtx;

    public DeleteContestCommandHandler(DbCtx dbCtx) => _dbCtx = dbCtx;

    public async ValueTask<Unit> Handle(DeleteContestCommand command, CancellationToken cancellationToken)
    {
        Contest contest = await _dbCtx.Contests.FindAsync(command.Id, cancellationToken)
                          ?? throw new Exception("Contest not found.");

        _dbCtx.Contests.Remove(contest);
        await _dbCtx.SaveChangesAsync(cancellationToken);

        return default;
    }
}
