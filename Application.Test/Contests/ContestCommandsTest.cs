using Application.Contests;
using Domain.Entities;
using FluentAssertions;

namespace Application.Test.Contests;

public class ContestCommandsTest
{
    private TestBed _tb = null!;

    [SetUp]
    public void SetUp() => _tb = TestBed.Instance;

    [TearDown]
    public void TearDown() => _tb.TearDown();

    [Test]
    public async ValueTask CreateContest()
    {
        var res = await _tb.Command(new CreateContestCommand("Name"));
        res.Name.Should().Be("Name");
        res.Status.Should().Be(ContestStatus.Draft);

        var contest = await _tb.DbCtx.Contests.FindAsync(res.Id);
        contest!.Name.Should().Be("Name");
        contest.Status.Should().Be(ContestStatus.Draft);
    }

    [Test]
    public async ValueTask UpdateContest()
    {
        var createRes = await _tb.Command(new CreateContestCommand("Old Name"));
        var updateRes = await _tb.Command(new UpdateContestCommand(createRes.Id, "New Name", DateTime.UtcNow.AddDays(10)));
        updateRes.Name.Should().Be("New Name");
        updateRes.Status.Should().Be(ContestStatus.Draft);
        updateRes.LockDate.Should().BeWithin(TimeSpan.FromSeconds(1)).Before(DateTime.UtcNow.AddDays(10));

        var contest = await _tb.DbCtx.Contests.FindAsync(updateRes.Id);
        contest!.Name.Should().Be("New Name");
        contest.Status.Should().Be(ContestStatus.Draft);
        contest.LockDate.Should().BeWithin(TimeSpan.FromSeconds(1)).Before(DateTime.UtcNow.AddDays(10));
    }

    [Test]
    public async ValueTask PublishContest()
    {
        var createRes = await _tb.Command(new CreateContestCommand("Name"));
        await _tb.Command(new UpdateContestCommand(createRes.Id, "Name", DateTime.UtcNow.AddDays(10)));
        var publishRes = await _tb.Command(new PublishContestCommand(createRes.Id));
        publishRes.Status.Should().Be(ContestStatus.Public);

        var contest = await _tb.DbCtx.Contests.FindAsync(publishRes.Id);
        contest!.Status.Should().Be(ContestStatus.Public);
    }

    [Test]
    public async ValueTask FinalizeContest()
    {
        var createRes = await _tb.Command(new CreateContestCommand("Name"));
        await _tb.Command(new UpdateContestCommand(createRes.Id, "Name", DateTime.UtcNow.AddDays(10)));
        await _tb.Command(new PublishContestCommand(createRes.Id));
        var contest = await _tb.DbCtx.Contests.FindAsync(createRes.Id);
        await _tb.DbCtx.Contestants.AddRangeAsync(Enumerable.Range(0, 10)
            .Select(_ => new Contestant
            {
                Contest = contest!,
                User = new User
                {
                    Username = "Username",
                },
            }));

        var finalize = await _tb.Command(new FinalizeContestCommand(createRes.Id));
        finalize.Status.Should().Be(ContestStatus.Finalized);
        contest!.Status.Should().Be(ContestStatus.Finalized);
    }

    [Test]
    public async ValueTask DeleteContest()
    {
        var createRes = await _tb.Command(new CreateContestCommand("Name"));
        await _tb.Command(new DeleteContestCommand(createRes.Id));

        var contest = await _tb.DbCtx.Contests.FindAsync(createRes.Id);
        contest.Should().BeNull();
    }
}
