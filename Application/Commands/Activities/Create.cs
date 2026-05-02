using Application.Validators;
using Application.Core;
using Application.Interfaces;
using Domain;
using Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Commands.Activities
{
  public class Create
  {
    public class Command : IRequest<Result<Unit>>
    {
      public Activity activity { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
      public CommandValidator()
      {
        RuleFor(x => x.activity).SetValidator(new ActivityValidator());
      }
    }

    public class Handler : IRequestHandler<Command, Result<Unit>>
    {
      private readonly IUserRepository _users;
      private readonly IActivityRepository _activities;
      private readonly IUnitOfWork _unitOfWork;
      private readonly IUserAccessor _userAccessor;

      public Handler(
        IUserRepository users,
        IActivityRepository activities,
        IUnitOfWork unitOfWork,
        IUserAccessor userAccessor)
      {
        _users = users;
        _activities = activities;
        _unitOfWork = unitOfWork;
        _userAccessor = userAccessor;
      }

      public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
      {
        var user = await _users.GetByUsernameAsync(_userAccessor.GetUsername(), cancellationToken);
        var attendee = new ActivityAttendee
        {
          AppUser = user,
          Activity = request.activity,
          IsHost = true
        };

        request.activity.Attendees.Add(attendee);
        _activities.Add(request.activity);

        var result = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;

        if (!result) return Result<Unit>.Failure("Failed to create activity");

        return Result<Unit>.Success(Unit.Value);
      }
    }
  }
}
