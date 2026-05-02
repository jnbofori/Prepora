using Application.Core;
using Application.DTOs.Recipes;
using Application.Interfaces;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Queries.Recipes
{
  public class Details
  {
    public class Query : IRequest<Result<RecipeDto>>
    {
      public Guid Id { get; set; }
    }

    public class Handler : IRequestHandler<Query, Result<RecipeDto>>
    {
      private readonly DataContext _context;
      private readonly IMapper _mapper;
      private readonly IUserAccessor _userAccessor;

      public Handler(DataContext context, IMapper mapper, IUserAccessor userAccessor)
      {
        _context = context;
        _mapper = mapper;
        _userAccessor = userAccessor;
      }

      public async Task<Result<RecipeDto>> Handle(Query request, CancellationToken cancellationToken)
      {
        var ownerId = _userAccessor.GetUserId();
        var recipe = await _context.Recipes
          .AsNoTracking()
          .Include(r => r.Ingredients)
          .Include(r => r.Steps)
          .Include(r => r.Tags)
          .Include(r => r.Photos)
          .FirstOrDefaultAsync(r => r.Id == request.Id && r.OwnerId == ownerId, cancellationToken);

        if (recipe == null) return null;

        return Result<RecipeDto>.Success(_mapper.Map<RecipeDto>(recipe));
      }
    }
  }
}
