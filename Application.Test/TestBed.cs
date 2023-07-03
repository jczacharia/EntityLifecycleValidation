using Application.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Test;

public class TestBed
{
    private static TestBed? s_instance;
    private static readonly object s_padlock = new object();

    private readonly IServiceCollection _serviceCollection;
    public readonly DbCtx DbCtx;

    private TestBed()
    {
        _serviceCollection = new ServiceCollection();
        _serviceCollection.AddLogging();
        _serviceCollection.AddMediator(o => o.ServiceLifetime = ServiceLifetime.Scoped);
        _serviceCollection.AddDbContext<DbCtx>(options =>
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            options.UseInMemoryDatabase("TestDb");
        });

        IServiceScope serviceScope = _serviceCollection.BuildServiceProvider().CreateScope();
        IServiceProvider serviceProvider = serviceScope.ServiceProvider;

        DbCtx = serviceProvider.GetRequiredService<DbCtx>();
        DbCtx.IgnoreLifecycleInterceptor = true;

        TearDown();
    }

    public static TestBed Instance
    {
        get
        {
            lock (s_padlock)
            {
                s_instance ??= new TestBed();
                return s_instance;
            }
        }
    }

    public void TearDown()
    {
        DbCtx.ChangeTracker.Clear();
        DbCtx.Database.EnsureDeleted();
        DbCtx.Database.EnsureCreated();
    }

    /// <summary>
    ///     Runs the given function in a scope with a new service provider.
    ///     This allows for a new DbCtx to be created for the scope
    ///     with a different ChangeTracker and the IgnoreLifecycleInterceptor set to false.
    /// </summary>
    public async ValueTask RunInScope(Func<IServiceProvider, ValueTask> func)
    {
        // Ensure we saved all data in the test bed
        await DbCtx.SaveChangesAsync();
        await using AsyncServiceScope scope = _serviceCollection.BuildServiceProvider().CreateAsyncScope();
        await func(scope.ServiceProvider);
    }

    public async ValueTask<TResponse> Query<TResponse>(IQuery<TResponse> request)
    {
        // Ensure we saved all data in the test bed
        await DbCtx.SaveChangesAsync();
        await using AsyncServiceScope scope = _serviceCollection.BuildServiceProvider().CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await sender.Send(request);
    }

    public async ValueTask<TResponse> Command<TResponse>(ICommand<TResponse> request)
    {
        // Ensure we saved all data in the test bed
        await DbCtx.SaveChangesAsync();
        await using AsyncServiceScope scope = _serviceCollection.BuildServiceProvider().CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        TResponse response = await sender.Send(request);

        // Reload all entities to ensure all entities in our test has updated data.
        // This is important since each request is scoped
        // but we are using a different DbContext instance in the tests.
        foreach (EntityEntry entity in DbCtx.ChangeTracker.Entries().ToList())
        {
            await entity.ReloadAsync();
        }

        return response;
    }

    public async ValueTask<TException> Fail<TException>(IMessage request)
        where TException : Exception
    {
        // Ensure we saved all data in the test bed
        await DbCtx.SaveChangesAsync();

        await using AsyncServiceScope scope = _serviceCollection.BuildServiceProvider().CreateAsyncScope();

        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        TException exception = null!;

        async Task RequestFunc()
        {
            try
            {
                await sender.Send(request);
            }
            catch (TException e)
            {
                exception = e;
                throw;
            }
        }

        Assert.ThrowsAsync<TException>(RequestFunc);

        // Just in case we updated some data in this fail.
        foreach (EntityEntry entity in DbCtx.ChangeTracker.Entries().ToList())
        {
            await entity.ReloadAsync();
        }

        return exception;
    }
}
