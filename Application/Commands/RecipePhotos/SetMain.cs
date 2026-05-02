using System.Linq;
using Application.Core;
using Application.Interfaces;
using Domain.Repositories;
using MediatR;

namespace Application.Commands.RecipePhotos
{
  public class SetMain
  {
    public class Command : IRequest<Result<Unit>>
    {
      public Guid RecipeId { get; set; }
      public string PhotoId { get; set; }
    }

    public class Handler : IRequestHandler<Command, Result<Unit>>
    {
      private readonly IRecipeRepository _recipes;
      private readonly IUserAccessor _userAccessor;
      private readonly IUnitOfWork _unitOfWork;

      public Handler(IRecipeRepository recipes, IUserAccessor userAccessor, IUnitOfWork unitOfWork)
      {
        _recipes = recipes;
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

        foreach (var p in recipe.Photos)
          p.IsCover = false;
        photo.IsCover = true;

        var ok = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
        if (!ok) return Result<Unit>.Failure("Failed to set cover photo");

        return Result<Unit>.Success(Unit.Value);
      }
    }
  }
}
