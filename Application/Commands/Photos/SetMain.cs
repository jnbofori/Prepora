using Application.Core;
using Application.Interfaces;
using Domain.Repositories;
using MediatR;

namespace Application.Commands.Photos
{
  public class SetMain
  {
    public class Command : IRequest<Result<Unit>>
    {
      public string Id { get; set; }
    }

    public class Handler : IRequestHandler<Command, Result<Unit>>
    {
      private readonly IUserAccessor _userAccessor;
      private readonly IUserRepository _users;
      private readonly IUnitOfWork _unitOfWork;

      public Handler(IUserRepository users, IUserAccessor userAccessor, IUnitOfWork unitOfWork)
      {
        _users = users;
        _userAccessor = userAccessor;
        _unitOfWork = unitOfWork;
      }

      public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
      {
        var user = await _users.GetByUsernameWithPhotosAsync(_userAccessor.GetUsername(), cancellationToken);
        if (user == null) return null;

        var photo = user.Photos.FirstOrDefault(x => x.Id == request.Id);
        if (photo == null) return null;

        var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);
        if (currentMain != null) currentMain.IsMain = false;
        photo.IsMain = true;

        var success = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
        if (success) return Result<Unit>.Success(Unit.Value);

        return Result<Unit>.Failure("Problem setting main photo");
      }
    }
  }
}
