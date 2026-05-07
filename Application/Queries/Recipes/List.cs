using Application.Core;
using Application.DTOs.Recipes;
using Application.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Queries.Recipes
{
  public class List
  {
    public class Query : IRequest<Result<PagedList<RecipeListDto>>>
    {
      public RecipeParams Params { get; set; }
    }

    public class Handler : IRequestHandler<Query, Result<PagedList<RecipeListDto>>>
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

      public async Task<Result<PagedList<RecipeListDto>>> Handle(Query request, CancellationToken cancellationToken)
      {
        var ownerId = _userAccessor.GetUserId();

        var query = _context.Recipes
          .AsNoTracking()
          .Where(r => r.OwnerId == ownerId);

        var search = request.Params.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
          var lowered = search.ToLower();
          query = query.Where(r =>
            r.Title.ToLower().Contains(lowered)
            || (r.Description != null && r.Description.ToLower().Contains(lowered)));
        }

        var tag = request.Params.Tag?.Trim();
        if (!string.IsNullOrEmpty(tag))
        {
          var tagLower = tag.ToLower();
          query = query.Where(r => r.Tags.Any(t => t.TagName.ToLower() == tagLower));
        }

        var projectedQuery = query
          .OrderByDescending(r => r.UpdatedUtc)
          .ProjectTo<RecipeListDto>(_mapper.ConfigurationProvider);

        return Result<PagedList<RecipeListDto>>.Success(
          await PagedList<RecipeListDto>.CreateAsync(projectedQuery, request.Params.PageNumber, request.Params.PageSize)
        );
      }
    }
  }
}
