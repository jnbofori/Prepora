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
