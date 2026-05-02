using System.Linq;
using Application.Core;
using Application.DTOs.Recipes;
using Application.Interfaces;
using Domain;
using Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Commands.RecipePhotos
{
  public class Add
  {
    public class Command : IRequest<Result<RecipePhotoDto>>
    {
      public Guid RecipeId { get; set; }
      public IFormFile File { get; set; }
    }

    public class Handler : IRequestHandler<Command, Result<RecipePhotoDto>>
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

      public async Task<Result<RecipePhotoDto>> Handle(Command request, CancellationToken cancellationToken)
      {
        var ownerId = _userAccessor.GetUserId();
        var recipe = await _recipes.GetByIdForOwnerAsync(request.RecipeId, ownerId, cancellationToken);
        if (recipe == null) return null;

        var upload = await _photoAccessor.AddPhoto(request.File);
        var maxOrder = recipe.Photos.Count > 0 ? recipe.Photos.Max(p => p.SortOrder) : -1;
        var photo = new RecipePhoto
        {
          Id = upload.PublicId,
          Url = upload.Url,
          SortOrder = maxOrder + 1,
          IsCover = !recipe.Photos.Any(p => p.IsCover)
        };

        recipe.Photos.Add(photo);

        var ok = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
        if (!ok) return Result<RecipePhotoDto>.Failure("Failed to save photo");

        return Result<RecipePhotoDto>.Success(new RecipePhotoDto
        {
          Id = photo.Id,
          Url = photo.Url,
          IsCover = photo.IsCover,
          SortOrder = photo.SortOrder
        });
      }
    }
  }
}
