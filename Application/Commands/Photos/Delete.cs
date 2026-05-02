using Application.Core;
using Application.Interfaces;
using Domain.Repositories;
using MediatR;

namespace Application.Commands.Photos
{
  public class Delete
  {
    public class Command : IRequest<Result<Unit>>
    {
      public string Id { get; set; }
    }

    public class Handler : IRequestHandler<Command, Result<Unit>>
    {
      private readonly IUserRepository _users;
      private readonly IPhotoAccessor _photoAccessor;
      private readonly IUserAccessor _userAccessor;
      private readonly IUnitOfWork _unitOfWork;

      public Handler(
        IUserRepository users,
        IPhotoAccessor photoAccessor,
        IUserAccessor userAccessor,
        IUnitOfWork unitOfWork)
      {
        _userAccessor = userAccessor;
        _photoAccessor = photoAccessor;
        _users = users;
        _unitOfWork = unitOfWork;
      }

      public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
      {
        var user = await _users.GetByUsernameWithPhotosAsync(_userAccessor.GetUsername(), cancellationToken);
        if (user == null) return null;

        var photo = user.Photos.FirstOrDefault(x => x.Id == request.Id);
        if (photo == null) return null;

        if (photo.IsMain) return Result<Unit>.Failure("You cannot delete your main photo");

        var result = await _photoAccessor.DeletePhoto(photo.Id);
        if (result == null) return Result<Unit>.Failure("Problem deleting photo form Cloudinary");

        user.Photos.Remove(photo);

        var success = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
        if (success) return Result<Unit>.Success(Unit.Value);

        return Result<Unit>.Failure("Problem deleting photo form Cloudinary");
      }
    }
  }
}
