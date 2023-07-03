using Application.Contests;
using Domain.Entities;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController, Route("contest")]
public class ContestController
{
    private readonly ISender _sender;

    public ContestController(ISender sender) => _sender = sender;

    [HttpPost]
    public ValueTask<Contest> CreateContest(CreateContestCommand command) => _sender.Send(command);

    [HttpPut]
    public ValueTask<Contest> UpdateContest(UpdateContestCommand command) => _sender.Send(command);

    [HttpPost("{id:int}/publish")]
    public ValueTask<Contest> PublishContest(int id) => _sender.Send(new PublishContestCommand(id));

    [HttpPost("{id:int}/finalize")]
    public ValueTask<Contest> FinalizeContest(int id) => _sender.Send(new FinalizeContestCommand(id));

    [HttpDelete("{id:int}")]
    public ValueTask<Unit> DeleteContest(int id) => _sender.Send(new DeleteContestCommand(id));
}
