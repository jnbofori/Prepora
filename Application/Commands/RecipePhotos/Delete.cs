using System.Linq;
using Application.Core;
using Application.Interfaces;
using Domain.Repositories;
using MediatR;

namespace Application.Commands.RecipePhotos
{
  public class Delete
  {
    public class Command : IRequest<Result<Unit>>
    {
      public Guid RecipeId { get; set; }
      public string PhotoId { get; set; }
    }

    public class Handler : IRequestHandler<Command, Result<Unit>>
    {
      private readonly IRecipeRepository _recipes;
      private readonly IPhotoAccessor _photoAccessor;
      private readonly IUserAccessor _userAccessor;
      private readonly IUnitOfWork _unitOfWork;

      public Handler(
        IRecipeRepository recipes,
        IPhotoAccessor photoAccessor,
        IUserAccessor userAccessor,
        IUnitOfWork unitOfWork)
      {
        _recipes = recipes;
        _photoAccessor = photoAccessor;
        _userAccessor = userAccessor;
        _unitOfWork = unitOfWork;
      }

      public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
      {
        var ownerId = _userAccessor.GetUserId();
        var recipe = await _recipes.GetByIdForOwnerAsync(request.RecipeId, ownerId, cancellationToken);
        if (recipe == null) return null;

        var photo = recipe.Photos.FirstOrDefault(p => p.Id == request.PhotoId);
        if (photo == null) return null;

        var deleted = await _photoAccessor.DeletePhoto(photo.Id);
        if (deleted == null) return Result<Unit>.Failure("Problem deleting photo from storage");

        var wasCover = photo.IsCover;
        recipe.Photos.Remove(photo);

        if (wasCover && recipe.Photos.Count > 0)
        {
          var next = recipe.Photos.OrderBy(p => p.SortOrder).First();
          next.IsCover = true;
        }

        var ok = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
        if (!ok) return Result<Unit>.Failure("Failed to update recipe");

        return Result<Unit>.Success(Unit.Value);
      }
    }
  }
}
