using Application.Core;
using Application.Interfaces;
using Domain;
using Domain.Repositories;
using MediatR;

namespace Application.Commands.Followers
{
  public class FollowToggle
  {
    public class Command : IRequest<Result<Unit>>
    {
      public string TargetUsername { get; set; }
    }

    public class Handler : IRequestHandler<Command, Result<Unit>>
    {
      private readonly IUserRepository _users;
      private readonly IUserFollowingRepository _followings;
      private readonly IUnitOfWork _unitOfWork;
      private readonly IUserAccessor _userAccessor;

      public Handler(
        IUserRepository users,
        IUserFollowingRepository followings,
        IUnitOfWork unitOfWork,
        IUserAccessor userAccessor)
      {
        _users = users;
        _followings = followings;
        _unitOfWork = unitOfWork;
        _userAccessor = userAccessor;
      }

      public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
      {
        var observer = await _users.GetByUsernameAsync(_userAccessor.GetUsername(), cancellationToken);
        if (observer == null) return null;

        var target = await _users.GetByUsernameAsync(request.TargetUsername, cancellationToken);
        if (target == null) return null;

        var following = await _followings.FindAsync(observer.Id, target.Id, cancellationToken);
        if (following == null)
        {
          following = new UserFollowing
          {
            Observer = observer,
            Target = target
          };

          _followings.Add(following);
        }
        else
        {
          _followings.Remove(following);
        }

        var success = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
        if (success) return Result<Unit>.Success(Unit.Value);

        return Result<Unit>.Failure("Failed to update following");
      }
    }
  }
}
