using Mediator;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Application.Persistence;

public record SavedChangesEvent(List<EntityEntry> Entries) : INotification;
