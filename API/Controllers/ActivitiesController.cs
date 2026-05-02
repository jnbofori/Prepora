using Domain;
using Microsoft.AspNetCore.Mvc;
using Persistence;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Application.DTOs.Activities;
using Application.Commands.Activities;
using Application.Queries.Activities;
using Microsoft.AspNetCore.Authorization;
using Application.Core;

namespace API.Controllers
{
  public class ActivitiesController: BaseApiController
  {
    // note: IAction result specifies http response (i think)
    [HttpGet]
    public async Task<IActionResult> GetActivities([FromQuery]ActivityParams activityParams) {
      // note: this sends query to mediator handler
      return HandlePagedResult(await Mediator.Send(new List.Query{Params = activityParams}));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetActivity(Guid id) {
      var result = await Mediator.Send(new Details.Query{ Id = id });

      return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateActivity(Activity activity) {
      return HandleResult(await Mediator.Send(new Create.Command{ activity = activity }) );
    }

    [Authorize(Policy = "IsActivityHost")]
    [HttpPut("{id}")]
    public async Task<IActionResult> EditActivity(Guid id, Activity activity) {
      activity.Id = id;
      return HandleResult(await Mediator.Send(new Edit.Command{ activity = activity }) );
    }

    [Authorize(Policy = "IsActivityHost")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteActivity(Guid id) {
      return HandleResult(await Mediator.Send(new Delete.Command{ Id = id }));
    }

    [HttpPost("{id}/attend")]
    public async Task<IActionResult> Attend(Guid id) {
      return HandleResult(await Mediator.Send(new UpdateAttendance.Command{ Id = id }) );
    }
  }
}