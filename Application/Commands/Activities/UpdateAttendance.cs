using Application.Core;
using Application.Interfaces;
using Domain;
using Domain.Repositories;
using MediatR;

namespace Application.Commands.Activities
{
  public class UpdateAttendance
  {
    public class Command : IRequest<Result<Unit>>
    {
      public Guid Id { get; set; }
    }

    public class Handler : IRequestHandler<Command, Result<Unit>>
    {
      private readonly IActivityRepository _activities;
      private readonly IUserRepository _users;
      private readonly IUnitOfWork _unitOfWork;
      private readonly IUserAccessor _userAccessor;

      public Handler(
        IActivityRepository activities,
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IUserAccessor userAccessor)
      {
        _activities = activities;
        _users = users;
        _unitOfWork = unitOfWork;
        _userAccessor = userAccessor;
      }

      public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
      {
        var activity = await _activities.GetByIdWithAttendeesAndUsersAsync(request.Id, cancellationToken);

        if (activity == null) return null;

        var user = await _users.GetByUsernameAsync(_userAccessor.GetUsername(), cancellationToken);

        if (user == null) return null;

        var hostUsername = activity.Attendees.FirstOrDefault(x => x.IsHost)?.AppUser?.UserName;

        var attendance = activity.Attendees.FirstOrDefault(x => x.AppUser.UserName == user.UserName);

        if (attendance != null && hostUsername == user.UserName)
          activity.IsCancelled = !activity.IsCancelled;

        if (attendance != null && hostUsername != user.UserName)
          activity.Attendees.Remove(attendance);

        if (attendance == null)
        {
          attendance = new ActivityAttendee
          {
            AppUser = user,
            Activity = activity,
            IsHost = false
          };
          activity.Attendees.Add(attendance);
        }

        var result = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
        return result ? Result<Unit>.Success(Unit.Value) : Result<Unit>.Failure("Problem updating attendance");
      }
    }
  }
}
