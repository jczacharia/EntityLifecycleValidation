using Application.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;

namespace Application.Contests;

public class ContestLifecycleHandler : EntityLifecycleHandler<Contest>
{
    private readonly DbCtx _dbCtx;
    private readonly ILogger<ContestLifecycleHandler> _logger;

    public ContestLifecycleHandler(ILogger<ContestLifecycleHandler> logger, DbCtx dbCtx)
    {
        _logger = logger;
        _dbCtx = dbCtx;
    }

    protected override ValueTask Added(EntityEntry<Contest> entry, CancellationToken cancellationToken)
    {
        Contest contest = entry.Entity;

        _logger.LogInformation("Detected new contest {ContestId}. Validating the contest is in Draft status", contest.Id);

        if (contest.Status is not ContestStatus.Draft)
        {
            throw new Exception("A contest can only be created in a draft state.");
        }

        _logger.LogInformation("Contest {ContestId} is in Draft status. Allowing the contest to be added to the database", contest.Id);

        return default;
    }

    protected override async ValueTask Modified(EntityEntry<Contest> entry, CancellationToken cancellationToken)
    {
        Contest contest = entry.Entity;
        PropertyEntry<Contest, ContestStatus> status = entry.Property(e => e.Status);

        DetectForbiddenStateTransitions(status);

        if (status is { OriginalValue: ContestStatus.Draft, CurrentValue: ContestStatus.Public })
        {
            DraftToPublic(contest);
        }

        if (status is { OriginalValue: ContestStatus.Public, CurrentValue: ContestStatus.Finalized })
        {
            await PublicToFinalized(contest, cancellationToken);
        }

        if (contest.Status is not ContestStatus.Draft)
        {
            CheckForbiddenPropertyModifications(entry);
        }
    }

    private static void DetectForbiddenStateTransitions(PropertyEntry<Contest, ContestStatus> status)
    {
        switch (status)
        {
            case { OriginalValue: ContestStatus.Public, CurrentValue: ContestStatus.Draft }:
                throw new Exception("A contest cannot be reverted to a draft status once it has been published.");

            case { OriginalValue: ContestStatus.Finalized, CurrentValue: ContestStatus.Draft }:
                throw new Exception("A contest cannot be reverted to a draft status once it has been finalized.");

            case { OriginalValue: ContestStatus.Draft, CurrentValue: ContestStatus.Finalized }:
                throw new Exception("A contest cannot be moved into a finalized status from a draft status.");

            case { OriginalValue: ContestStatus.Finalized, CurrentValue: ContestStatus.Public }:
                throw new Exception("A contest cannot be reverted to a public status once it has been finalized.");
        }
    }

    private void DraftToPublic(Contest contest)
    {
        _logger.LogInformation("Detected contest {ContestId} is being published. Validating the lock date is at least three days in the future", contest.Id);

        if (contest.LockDate < DateTime.UtcNow.AddDays(3))
        {
            throw new Exception("A contest can only be published if the lock date is at least three days in the future.");
        }

        _logger.LogInformation("Contest {ContestId} has a valid lock date. Allowing the contest to be published", contest.Id);
    }

    private async ValueTask PublicToFinalized(Contest contest, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Detected contest {ContestId} is being finalized. Validating the contest lock date has passed and has at least 10 contestants", contest.Id);

        if (contest.LockDate > DateTime.UtcNow)
        {
            throw new Exception("A contest can only be finalized if the lock date has passed.");
        }

        int contestantsCount = await _dbCtx.Contestants.CountAsync(c => c.Contest.Id == contest.Id, cancellationToken);
        if (contestantsCount < 10)
        {
            throw new Exception("A contest can only be finalized if it has at least 10 contestants.");
        }

        _logger.LogInformation("Contest {ContestId} lock date has passed and has at least 10 contestants. Allowing the contest to be finalized", contest.Id);
    }

    private void CheckForbiddenPropertyModifications(EntityEntry<Contest> entry)
    {
        _logger.LogInformation("Detected a modified contest {ContestId} that is not draft, checking for forbidden property modifications", entry.Entity.Id);

        PropertyEntry<Contest, string> name = entry.Property(e => e.Name);
        if (name.IsModified)
        {
            throw new Exception("A contest's name cannot be modified once the contest has been published.");
        }

        PropertyEntry<Contest, DateTime> lockDate = entry.Property(e => e.LockDate);
        if (lockDate.IsModified)
        {
            throw new Exception("A contest's lock date cannot be modified once the contest has been published.");
        }

        _logger.LogInformation("Contest {ContestId} has no forbidden property modifications. Allowing the contest to be modified", entry.Entity.Id);
    }

    protected override async ValueTask Deleted(EntityEntry<Contest> entry, CancellationToken cancellationToken)
    {
        Contest contest = entry.Entity;

        _logger.LogInformation("Detected deletion of Contest {ContestId}. Validating that there are no contestants", contest.Id);

        bool hasContestants = await _dbCtx.Contestants.AnyAsync(c => c.Contest.Id == entry.Entity.Id, cancellationToken);
        if (hasContestants)
        {
            throw new Exception("A contest cannot be deleted if it has contestants.");
        }

        _logger.LogInformation("Contest {ContestId} has no contestants. Allowing the contest to be deleted", contest.Id);
    }
}
