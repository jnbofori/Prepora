using Application.Core;
using Application.Interfaces;
using Domain;
using Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Commands.Photos
{
  public class Add
  {
    public class Command : IRequest<Result<Photo>>
    {
      public IFormFile File { get; set; }
    }

    public class Handler : IRequestHandler<Command, Result<Photo>>
    {
      private readonly IUserAccessor _userAccessor;
      private readonly IPhotoAccessor _photoAccessor;
      private readonly IUserRepository _users;
      private readonly IUnitOfWork _unitOfWork;

      public Handler(
        IUserRepository users,
        IPhotoAccessor photoAccessor,
        IUserAccessor userAccessor,
        IUnitOfWork unitOfWork)
      {
        _photoAccessor = photoAccessor;
        _userAccessor = userAccessor;
        _users = users;
        _unitOfWork = unitOfWork;
      }

      public async Task<Result<Photo>> Handle(Command request, CancellationToken cancellationToken)
      {
        var user = await _users.GetByUsernameWithPhotosAsync(_userAccessor.GetUsername(), cancellationToken);

        if (user == null) return null;

        var photoUploadResult = await _photoAccessor.AddPhoto(request.File);

        var photo = new Photo
        {
          Url = photoUploadResult.Url,
          Id = photoUploadResult.PublicId
        };

        if (!user.Photos.Any(x => x.IsMain)) photo.IsMain = true;

        user.Photos.Add(photo);

        var result = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;

        if (result) return Result<Photo>.Success(photo);

        return Result<Photo>.Failure("Problem adding photo");
      }
    }
  }
}
