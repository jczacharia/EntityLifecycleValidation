using Mediator;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Application.Persistence;

public record SavingChangesEvent(List<EntityEntry> Entries) : INotification;
