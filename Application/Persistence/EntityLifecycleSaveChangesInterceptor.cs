using Mediator;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Application.Persistence;

public class EntityLifecycleSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;

    public EntityLifecycleSaveChangesInterceptor(IPublisher publisher) => _publisher = publisher;

    /// <summary>
    /// Flag to prevent infinite recursion, allowing the entity lifecycle middleware to call SaveChangesAsync.
    /// </summary>
    private bool IsSaving { get; set; }

    /// <summary>
    /// Flag to prevent infinite recursion, allowing the entity lifecycle middleware to call SaveChangesAsync.
    /// </summary>
    private bool IsFinalizing { get; set; }

    // Throw exception if SaveChanges is called directly, we want to enforce using async code only.
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) =>
        throw new InvalidOperationException("SaveChangesAsync only");

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is DbCtx { IgnoreLifecycleInterceptor: true })
        {
            return result;
        }

        if (IsSaving)
        {
            return result;
        }

        IsSaving = true;

        List<EntityEntry> entityEntries = eventData.Context!.ChangeTracker.Entries().ToList();
        await _publisher.Publish(new SavingChangesEvent(entityEntries), cancellationToken);

        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is DbCtx { IgnoreLifecycleInterceptor: true })
        {
            return result;
        }

        if (IsFinalizing)
        {
            return result;
        }

        IsFinalizing = true;

        List<EntityEntry> entityEntries = eventData.Context!.ChangeTracker.Entries().ToList();
        await _publisher.Publish(new SavedChangesEvent(entityEntries), cancellationToken);

        IsFinalizing = false;
        IsSaving = false;

        return result;
    }
}
