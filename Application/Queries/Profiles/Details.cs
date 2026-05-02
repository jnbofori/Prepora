using Application.Core;
using Application.Interfaces;
using AutoMapper;
using UserProfile = Application.DTOs.Profiles.Profile;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Queries.Profiles
{
  public class Details
  {
    public class Query : IRequest<Result<UserProfile>>
    {
      public string Username { get; set; }
    }

    public class Handler : IRequestHandler<Query, Result<UserProfile>>
    {
      private readonly DataContext _context;
      private readonly IMapper _mapper;
      private readonly IUserAccessor _userAccessor;

      public Handler(DataContext context, IMapper mapper, IUserAccessor userAccessor)
      {
        _userAccessor = userAccessor;
        _mapper = mapper;
        _context = context;
      }

      public async Task<Result<UserProfile>> Handle(Query request, CancellationToken cancellationToken)
      {
        var user = await _context.Users
          .ProjectTo<UserProfile>(_mapper.ConfigurationProvider, new { currentUsername = _userAccessor.GetUsername() })
          .SingleOrDefaultAsync(x => x.Username == request.Username, cancellationToken);
        if (user == null) return null;

        return Result<UserProfile>.Success(user);
      }
    }
  }
}
