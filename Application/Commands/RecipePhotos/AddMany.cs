using Application.Core;
using Application.Interfaces;
using Domain;
using Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Commands.RecipePhotos
{
  public class AddMany
  {
    public class Command : IRequest<Result<Unit>>
    {
      public Guid RecipeId { get; set; }
      public ICollection<IFormFile> Files { get; set; } = new List<IFormFile>();
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

        var files = request.Files.Where(f => f != null && f.Length > 0).ToList();
        if (!files.Any()) return Result<Unit>.Failure("At least one photo is required");

        var nextSortOrder = recipe.Photos.Count > 0 ? recipe.Photos.Max(p => p.SortOrder) + 1 : 0;
        var shouldSetCover = !recipe.Photos.Any(p => p.IsCover);

        foreach (var file in files)
        {
          var upload = await _photoAccessor.AddPhoto(file);
          if (upload == null) return Result<Unit>.Failure("Failed to upload photo");

          recipe.Photos.Add(new RecipePhoto
          {
            Id = upload.PublicId,
            Url = upload.Url,
            SortOrder = nextSortOrder++,
            IsCover = shouldSetCover
          });

          shouldSetCover = false;
        }

        var ok = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
        if (!ok) return Result<Unit>.Failure("Failed to save photos");

        return Result<Unit>.Success(Unit.Value);
      }
    }
  }
}
