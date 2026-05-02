using Application.Commands.Comments;
using Application.Queries.Comments;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
  // note: signalR comes with .NET runtime by default
  public class ChatHub : Hub
  {
    private readonly IMediator _mediator;
    public ChatHub(IMediator mediator)
    {
      _mediator = mediator;
    }

    public async Task SendComment(Create.Command command)
    {
      var comment = await _mediator.Send(command);

      // note: within the Hub Context we have access to connected clients via the 'Clients' object.
      // Our Groups in our case represent each activity
      // Over here we're sending the new comment to everybody in the group
      await Clients.Group(command.ActivityId.ToString())
        .SendAsync("ReceiveComment", comment.Value);
    }

    // note: when client joins our hub, we want them to join a group
    public override async Task OnConnectedAsync()
    {
      // note: when client connects we add him/her to group
      var httpContext = Context.GetHttpContext();
      var activityId = httpContext.Request.Query["activityId"];
      await Groups.AddToGroupAsync(Context.ConnectionId, activityId);
      var result = await _mediator.Send(new List.Query{ActivityId = Guid.Parse(activityId)});
      await Clients.Caller.SendAsync("LoadComments", result.Value);

      // note: no need to handle disconnect cos once client disconnects the ConnectionId
      // will be removed from the group
    }
  }
}