using Application.DTOs.Comments;
using Application.Core;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Queries.Comments
{
  public class List
  {
    public class Query : IRequest<Result<List<CommentDto>>>
    {
      public Guid ActivityId { get; set; }
    }

    public class Handler : IRequestHandler<Query, Result<List<CommentDto>>>
    {
      private readonly DataContext _context;
      private readonly IMapper _mapper;

      public Handler(DataContext context, IMapper mapper)
      {
        _context = context;
        _mapper = mapper;
      }

      public async Task<Result<List<CommentDto>>> Handle(Query request, CancellationToken cancellationToken)
      {
        var comments = await _context.Comments
          .Where(x => x.Activity.Id == request.ActivityId)
          .OrderByDescending(x => x.CreatedAt)
          .ProjectTo<CommentDto>(_mapper.ConfigurationProvider)
          .ToListAsync(cancellationToken);

        return Result<List<CommentDto>>.Success(comments);
      }
    }
  }
}
