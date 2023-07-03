using Application.Persistence;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Test.Contests;

public class ContestLifecycleTest
{
    private TestBed _tb = null!;

    [SetUp]
    public void SetUp() => _tb = TestBed.Instance;

    [TearDown]
    public void TearDown() => _tb.TearDown();

    [Test]
    public async ValueTask MustBeAddedInDraftState()
    {
        Contest contest = new Contest
        {
            Name = "New Contest", Status = ContestStatus.Public,
        };

        await _tb.RunInScope(async sp =>
        {
            DbCtx dbCtx = sp.GetRequiredService<DbCtx>();
            await dbCtx.Contests.AddAsync(contest);
            Exception? result = Assert.ThrowsAsync<Exception>(() => dbCtx.SaveChangesAsync());
            result!.Message.Should().Be("A contest can only be created in a draft state.");

            contest.Status = ContestStatus.Draft;
            Assert.DoesNotThrowAsync(() => dbCtx.SaveChangesAsync());
        });
    }

    [Test]
    public async ValueTask PublishedContestMustHaveLockDate3DaysAway()
    {
        Contest contest = new Contest
        {
            Name = "New Contest",
        };
        await _tb.DbCtx.Contests.AddAsync(contest);

        await _tb.RunInScope(sp =>
        {
            DbCtx dbCtx = sp.GetRequiredService<DbCtx>();
            dbCtx.Contests.Attach(contest);
            contest.Status = ContestStatus.Public;
            contest.LockDate = DateTime.UtcNow.AddDays(2);
            Exception? result = Assert.ThrowsAsync<Exception>(() => dbCtx.SaveChangesAsync());
            result!.Message.Should().Be("A contest can only be published if the lock date is at least three days in the future.");

            contest.LockDate = DateTime.UtcNow.AddDays(3);
            Assert.DoesNotThrowAsync(() => dbCtx.SaveChangesAsync());
            return default;
        });
    }

    [Test]
    public async ValueTask CannotBeRevertedToDraftFromPublic()
    {
        Contest contest = new Contest
        {
            Name = "New Contest", Status = ContestStatus.Public,
        };
        await _tb.DbCtx.Contests.AddAsync(contest);

        await _tb.RunInScope(sp =>
        {
            DbCtx dbCtx = sp.GetRequiredService<DbCtx>();
            dbCtx.Contests.Attach(contest);
            contest.Status = ContestStatus.Draft;
            Exception? result = Assert.ThrowsAsync<Exception>(() => dbCtx.SaveChangesAsync());
            result!.Message.Should().Be("A contest cannot be reverted to a draft status once it has been published.");
            return default;
        });
    }

    [Test]
    public async ValueTask CannotBeRevertedToDraftFromFinalized()
    {
        Contest contest = new Contest
        {
            Name = "New Contest", Status = ContestStatus.Finalized,
        };
        await _tb.DbCtx.Contests.AddAsync(contest);

        await _tb.RunInScope(sp =>
        {
            DbCtx dbCtx = sp.GetRequiredService<DbCtx>();
            dbCtx.Contests.Attach(contest);
            contest.Status = ContestStatus.Draft;
            Exception? result = Assert.ThrowsAsync<Exception>(() => dbCtx.SaveChangesAsync());
            result!.Message.Should().Be("A contest cannot be reverted to a draft status once it has been finalized.");
            return default;
        });
    }

    [Test]
    public async ValueTask CannotBeRevertedToPublic()
    {
        Contest contest = new Contest
        {
            Name = "New Contest", Status = ContestStatus.Finalized,
        };
        await _tb.DbCtx.Contests.AddAsync(contest);

        await _tb.RunInScope(sp =>
        {
            DbCtx dbCtx = sp.GetRequiredService<DbCtx>();
            dbCtx.Contests.Attach(contest);
            contest.Status = ContestStatus.Public;
            Exception? result = Assert.ThrowsAsync<Exception>(() => dbCtx.SaveChangesAsync());
            result!.Message.Should().Be("A contest cannot be reverted to a public status once it has been finalized.");
            return default;
        });
    }

    [Test]
    public async ValueTask CannotGoFromDraftToFinalized()
    {
        Contest contest = new Contest
        {
            Name = "New Contest", Status = ContestStatus.Draft,
        };
        await _tb.DbCtx.Contests.AddAsync(contest);

        await _tb.RunInScope(sp =>
        {
            DbCtx dbCtx = sp.GetRequiredService<DbCtx>();
            dbCtx.Contests.Attach(contest);
            contest.Status = ContestStatus.Finalized;
            Exception? result = Assert.ThrowsAsync<Exception>(() => dbCtx.SaveChangesAsync());
            result!.Message.Should().Be("A contest cannot be moved into a finalized status from a draft status.");
            return default;
        });
    }

    [Test]
    public async ValueTask CannotBeFinalizedUntil10Contestants()
    {
        Contest contest = new Contest
        {
            Name = "New Contest", Status = ContestStatus.Public,
        };
        await _tb.DbCtx.Contests.AddAsync(contest);

        await _tb.RunInScope(async sp =>
        {
            DbCtx dbCtx = sp.GetRequiredService<DbCtx>();
            dbCtx.Contests.Attach(contest);
            contest.Status = ContestStatus.Finalized;
            Exception? result = Assert.ThrowsAsync<Exception>(() => dbCtx.SaveChangesAsync());
            result!.Message.Should().Be("A contest can only be finalized if it has at least 10 contestants.");

            await dbCtx.Contestants.AddRangeAsync(Enumerable.Range(0, 10)
                .Select(_ => new Contestant
                {
                    Contest = contest,
                    User = new User
                    {
                        Username = "Username",
                    },
                }));

            Assert.DoesNotThrowAsync(() => dbCtx.SaveChangesAsync());
        });
    }

    [Test]
    public async ValueTask CannotBeDeletedIfContestantsExist()
    {
        Contest contest = new Contest
        {
            Name = "New Contest",
        };
        await _tb.DbCtx.Contests.AddAsync(contest);

        Contestant contestant = new Contestant
        {
            Contest = contest,
            User = new User
            {
                Username = "Username",
            },
        };
        await _tb.DbCtx.Contestants.AddAsync(contestant);

        await _tb.RunInScope(sp =>
        {
            DbCtx dbCtx = sp.GetRequiredService<DbCtx>();
            dbCtx.Contests.Attach(contest);
            dbCtx.Contestants.Attach(contestant);
            dbCtx.Contests.Remove(contest);
            Exception? result = Assert.ThrowsAsync<Exception>(() => dbCtx.SaveChangesAsync());
            result!.Message.Should().Be("A contest cannot be deleted if it has contestants.");

            dbCtx.Contestants.Remove(contestant);
            Assert.DoesNotThrowAsync(() => dbCtx.SaveChangesAsync());
            return default;
        });
    }

    [Test]
    public async ValueTask ContestNameCannotBeModifiedAfterPublished()
    {
        Contest contest = new Contest
        {
            Name = "New Contest", Status = ContestStatus.Public,
        };
        await _tb.DbCtx.Contests.AddAsync(contest);

        await _tb.RunInScope(sp =>
        {
            DbCtx dbCtx = sp.GetRequiredService<DbCtx>();
            dbCtx.Contests.Attach(contest);
            contest.Name = "New Name";
            Exception? result = Assert.ThrowsAsync<Exception>(() => dbCtx.SaveChangesAsync());
            result!.Message.Should().Be("A contest's name cannot be modified once the contest has been published.");
            return default;
        });
    }

    [Test]
    public async ValueTask ContestLockCannotBeModifiedAfterPublished()
    {
        Contest contest = new Contest
        {
            Name = "New Contest", Status = ContestStatus.Finalized,
        };
        await _tb.DbCtx.Contests.AddAsync(contest);

        await _tb.RunInScope(sp =>
        {
            DbCtx dbCtx = sp.GetRequiredService<DbCtx>();
            dbCtx.Contests.Attach(contest);
            contest.LockDate = DateTime.UtcNow.AddDays(10);
            Exception? result = Assert.ThrowsAsync<Exception>(() => dbCtx.SaveChangesAsync());
            result!.Message.Should().Be("A contest's lock date cannot be modified once the contest has been published.");
            return default;
        });
    }
}
