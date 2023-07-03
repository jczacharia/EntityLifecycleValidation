using Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Application.Persistence;

public class DbCtx : DbContext
{
    private readonly IPublisher _publisher;

    public DbCtx(DbContextOptions<DbCtx> options, IPublisher publisher) : base(options) => _publisher = publisher;
    public DbSet<User> Users { get; set; } = default!;
    public DbSet<Contest> Contests { get; set; } = default!;
    public DbSet<Contestant> Contestants { get; set; } = default!;

    /// <summary>
    ///     This will bypass the entity lifecycle interceptor that
    ///     ensures entity validation. Only use this for testing.
    ///     Never set this to true in development and production.
    /// </summary>
    public bool IgnoreLifecycleInterceptor { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.AddInterceptors(new EntityLifecycleSaveChangesInterceptor(_publisher));
}
