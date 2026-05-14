using Application.Core;
using Application.DTOs.Recipes;
using Application.Interfaces;
using Application.Validators;
using Domain;
using Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Commands.Recipes
{
  public class Edit
  {
    public class Command : IRequest<Result<Unit>>
    {
      public Guid Id { get; set; }
      public RecipeUpsertDto Recipe { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
      public CommandValidator()
      {
        RuleFor(x => x.Recipe).SetValidator(new RecipeUpsertValidator());
      }
    }

    public class Handler : IRequestHandler<Command, Result<Unit>>
    {
      private readonly IRecipeRepository _recipes;
      private readonly IUnitOfWork _unitOfWork;
      private readonly IUserAccessor _userAccessor;
      private readonly INutritionService _nutritionService;

      public Handler(
        IRecipeRepository recipes,
        IUnitOfWork unitOfWork,
        IUserAccessor userAccessor,
        INutritionService nutritionService)
      {
        _recipes = recipes;
        _unitOfWork = unitOfWork;
        _userAccessor = userAccessor;
        _nutritionService = nutritionService;
      }

      public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
      {
        var ownerId = _userAccessor.GetUserId();
        var recipe = await _recipes.GetByIdForOwnerAsync(request.Id, ownerId, cancellationToken);
        if (recipe == null) return null;

        recipe.Title = request.Recipe.Title.Trim();
        recipe.Description = request.Recipe.Description?.Trim();
        recipe.SourceUrl = string.IsNullOrWhiteSpace(request.Recipe.SourceUrl) ? null : request.Recipe.SourceUrl.Trim();
        recipe.PrepMinutes = request.Recipe.PrepMinutes;
        recipe.CookMinutes = request.Recipe.CookMinutes;
        recipe.TotalMinutes = request.Recipe.TotalMinutes;
        recipe.Servings = request.Recipe.Servings;
        recipe.UpdatedUtc = DateTime.UtcNow;

        var oldIngredients = recipe.Ingredients.ToList();
        recipe.Ingredients.Clear();
        _recipes.RemoveIngredients(oldIngredients);
        var newIngredients = request.Recipe.Ingredients
          .OrderBy(i => i.SortOrder)
          .Select(ing => new RecipeIngredient
          {
            Id = Guid.NewGuid(),
            RecipeId = recipe.Id,
            SortOrder = ing.SortOrder,
            Name = ing.Name.Trim(),
            Quantity = ing.Quantity,
            Unit = string.IsNullOrWhiteSpace(ing.Unit) ? null : ing.Unit.Trim(),
            Note = string.IsNullOrWhiteSpace(ing.Note) ? null : ing.Note.Trim()
          })
          .ToList();
        _recipes.AddIngredients(newIngredients);

        var oldSteps = recipe.Steps.ToList();
        recipe.Steps.Clear();
        _recipes.RemoveSteps(oldSteps);
        var newSteps = request.Recipe.Steps
          .OrderBy(s => s.SortOrder)
          .Select(step => new RecipeStep
          {
            Id = Guid.NewGuid(),
            RecipeId = recipe.Id,
            SortOrder = step.SortOrder,
            Instruction = step.Instruction.Trim()
          })
          .ToList();
        _recipes.AddSteps(newSteps);

        var oldTags = recipe.Tags.ToList();
        recipe.Tags.Clear();
        _recipes.RemoveTags(oldTags);
        var newTags = request.Recipe.Tags
          .Where(t => !string.IsNullOrWhiteSpace(t))
          .Distinct(StringComparer.OrdinalIgnoreCase)
          .Select(tag => new RecipeTag
          {
            Id = Guid.NewGuid(),
            RecipeId = recipe.Id,
            TagName = tag.Trim()
          })
          .ToList();
        _recipes.AddTags(newTags);

        var nutrition = await _nutritionService.CalculateRecipeAsync(newIngredients, recipe.Servings, cancellationToken);
        RecipeNutritionUpdater.Apply(recipe, nutrition, newIngredients);

        var ok = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
        if (!ok) return Result<Unit>.Failure("Failed to update recipe");

        return Result<Unit>.Success(Unit.Value);
      }
    }
  }
}
