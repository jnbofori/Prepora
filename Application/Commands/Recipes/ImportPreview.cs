using Application.Core;
using Application.DTOs.Recipes;
using Application.Interfaces;
using FluentValidation;
using MediatR;

namespace Application.Commands.Recipes
{
  public class ImportPreview
  {
    public class Command : IRequest<Result<RecipeImportPreviewDto>>
    {
      public string Url { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
      public CommandValidator()
      {
        RuleFor(x => x.Url)
          .NotEmpty()
          .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
          .WithMessage("Url must be a valid absolute https URL");
      }
    }

    public class Handler : IRequestHandler<Command, Result<RecipeImportPreviewDto>>
    {
      private readonly IRecipeImportService _importService;

      public Handler(IRecipeImportService importService)
      {
        _importService = importService;
      }

      public async Task<Result<RecipeImportPreviewDto>> Handle(Command request, CancellationToken cancellationToken)
      {
        var preview = await _importService.ImportPreviewAsync(request.Url, cancellationToken);
        return Result<RecipeImportPreviewDto>.Success(preview);
      }
    }
  }
}
