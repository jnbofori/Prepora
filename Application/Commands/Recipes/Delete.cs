using Application.Core;
using Application.Interfaces;
using Domain.Repositories;
using MediatR;

namespace Application.Commands.Recipes
{
  public class Delete
  {
    public class Command : IRequest<Result<Unit>>
    {
      public Guid Id { get; set; }
    }

    public class Handler : IRequestHandler<Command, Result<Unit>>
    {
      private readonly IRecipeRepository _recipes;
      private readonly IUnitOfWork _unitOfWork;
      private readonly IUserAccessor _userAccessor;

      public Handler(IRecipeRepository recipes, IUnitOfWork unitOfWork, IUserAccessor userAccessor)
      {
        _recipes = recipes;
        _unitOfWork = unitOfWork;
        _userAccessor = userAccessor;
      }

      public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
      {
        var ownerId = _userAccessor.GetUserId();
        var recipe = await _recipes.GetByIdForOwnerAsync(request.Id, ownerId, cancellationToken);
        if (recipe == null) return null;

        _recipes.Remove(recipe);

        var ok = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
        if (!ok) return Result<Unit>.Failure("Failed to delete recipe");

        return Result<Unit>.Success(Unit.Value);
      }
    }
  }
}
