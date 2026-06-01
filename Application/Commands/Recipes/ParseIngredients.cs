using Application.Core;
using Application.DTOs.Recipes;
using Application.Interfaces;
using FluentValidation;
using MediatR;

namespace Application.Commands.Recipes
{
  public class ParseIngredients
  {
    public class Command : IRequest<Result<ParseIngredientsResultDto>>
    {
      public ParseIngredientsRequest Request { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
      public CommandValidator()
      {
        RuleFor(x => x.Request).NotNull();
        RuleFor(x => x)
          .Must(x => HasInput(x.Request))
          .WithMessage("Provide at least one ingredient line in Lines or Text");
      }

      private static bool HasInput(ParseIngredientsRequest request)
      {
        if (request == null) return false;
        if (request.Lines != null && request.Lines.Any(l => !string.IsNullOrWhiteSpace(l))) return true;
        return !string.IsNullOrWhiteSpace(request.Text);
      }
    }

    public class Handler : IRequestHandler<Command, Result<ParseIngredientsResultDto>>
    {
      private readonly IIngredientParserService _parserService;

      public Handler(IIngredientParserService parserService)
      {
        _parserService = parserService;
      }

      public async Task<Result<ParseIngredientsResultDto>> Handle(Command request, CancellationToken cancellationToken)
      {
        var lines = NormalizeLines(request.Request);
        if (!lines.Any())
          return Result<ParseIngredientsResultDto>.Failure("At least one ingredient line is required");

        var result = await _parserService.ParseAsync(lines, cancellationToken);
        return Result<ParseIngredientsResultDto>.Success(result);
      }

      private static List<string> NormalizeLines(ParseIngredientsRequest request)
      {
        if (request.Lines != null && request.Lines.Any(l => !string.IsNullOrWhiteSpace(l)))
          return request.Lines.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()).ToList();

        if (string.IsNullOrWhiteSpace(request.Text)) return new List<string>();

        return request.Text
          .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
          .Select(l => l.Trim())
          .Where(l => l.Length > 0)
          .ToList();
      }
    }
  }
}
