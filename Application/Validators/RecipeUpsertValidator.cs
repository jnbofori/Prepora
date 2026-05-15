using Application.DTOs.Recipes;
using FluentValidation;

namespace Application.Validators
{
  public class RecipeUpsertValidator : AbstractValidator<RecipeUpsertDto>
  {
    public RecipeUpsertValidator()
    {
      RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
      RuleFor(x => x.Description).MaximumLength(8000);
      RuleFor(x => x.SourceUrl)
        .Must(u => string.IsNullOrWhiteSpace(u) || Uri.TryCreate(u, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
        .WithMessage("SourceUrl must be a valid https URL");
      RuleFor(x => x.Servings).GreaterThan(0);
      RuleFor(x => x.PrepMinutes).GreaterThanOrEqualTo(0).When(x => x.PrepMinutes.HasValue);
      RuleFor(x => x.CookMinutes).GreaterThanOrEqualTo(0).When(x => x.CookMinutes.HasValue);
      RuleFor(x => x.TotalMinutes).GreaterThanOrEqualTo(0).When(x => x.TotalMinutes.HasValue);

      RuleFor(x => x.Calories).GreaterThanOrEqualTo(0).When(x => x.Calories.HasValue);
      RuleFor(x => x.ProteinGrams).GreaterThanOrEqualTo(0).When(x => x.ProteinGrams.HasValue);
      RuleFor(x => x.CarbsGrams).GreaterThanOrEqualTo(0).When(x => x.CarbsGrams.HasValue);
      RuleFor(x => x.FatGrams).GreaterThanOrEqualTo(0).When(x => x.FatGrams.HasValue);
      RuleFor(x => x.CaloriesPerServing).GreaterThanOrEqualTo(0).When(x => x.CaloriesPerServing.HasValue);
      RuleFor(x => x.ProteinGramsPerServing).GreaterThanOrEqualTo(0).When(x => x.ProteinGramsPerServing.HasValue);
      RuleFor(x => x.CarbsGramsPerServing).GreaterThanOrEqualTo(0).When(x => x.CarbsGramsPerServing.HasValue);
      RuleFor(x => x.FatGramsPerServing).GreaterThanOrEqualTo(0).When(x => x.FatGramsPerServing.HasValue);

      RuleForEach(x => x.Ingredients).SetValidator(new RecipeUpsertIngredientValidator());
      RuleForEach(x => x.Steps).SetValidator(new RecipeUpsertStepValidator());
      RuleForEach(x => x.Tags).MaximumLength(100);
    }
  }

  public class RecipeUpsertIngredientValidator : AbstractValidator<RecipeUpsertIngredientDto>
  {
    public RecipeUpsertIngredientValidator()
    {
      RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
      RuleFor(x => x.Unit).MaximumLength(100);
      RuleFor(x => x.Note).MaximumLength(500);
      RuleFor(x => x.Calories).GreaterThanOrEqualTo(0).When(x => x.Calories.HasValue);
      RuleFor(x => x.ProteinGrams).GreaterThanOrEqualTo(0).When(x => x.ProteinGrams.HasValue);
      RuleFor(x => x.CarbsGrams).GreaterThanOrEqualTo(0).When(x => x.CarbsGrams.HasValue);
      RuleFor(x => x.FatGrams).GreaterThanOrEqualTo(0).When(x => x.FatGrams.HasValue);
    }
  }

  public class RecipeUpsertStepValidator : AbstractValidator<RecipeUpsertStepDto>
  {
    public RecipeUpsertStepValidator()
    {
      RuleFor(x => x.Instruction).NotEmpty().MaximumLength(4000);
    }
  }
}
