using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Application.Persistence;

public abstract class EntityLifeCycleHandler<TEntity> : INotificationHandler<SavingChangesEvent> where TEntity : class
{
    public async ValueTask Handle(SavingChangesEvent notification, CancellationToken cancellationToken)
    {
        foreach (EntityEntry entity in notification.Entries.Where(e => e.Entity is TEntity))
        {
            switch (entity.State)
            {
                case EntityState.Added:
                    await Added(entity.Context.Entry((TEntity)entity.Entity), cancellationToken);
                    break;

                case EntityState.Modified:
                    await Modified(entity.Context.Entry((TEntity)entity.Entity), cancellationToken);
                    break;

                case EntityState.Deleted:
                    await Deleted(entity.Context.Entry((TEntity)entity.Entity), cancellationToken);
                    break;
            }
        }
    }

    protected virtual ValueTask Added(EntityEntry<TEntity> entry, CancellationToken cancellationToken) => default;
    protected virtual ValueTask Modified(EntityEntry<TEntity> entry, CancellationToken cancellationToken) => default;
    protected virtual ValueTask Deleted(EntityEntry<TEntity> entry, CancellationToken cancellationToken) => default;
}
