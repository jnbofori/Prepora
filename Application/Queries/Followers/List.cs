using Application.Core;
using Application.Interfaces;
using AutoMapper;
using UserProfile = Application.DTOs.Profiles.Profile;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Queries.Followers
{
  public class List
  {
    public class Query : IRequest<Result<List<UserProfile>>>
    {
      public string Predicate { get; set; }
      public string Username { get; set; }
    }

    public class Handler : IRequestHandler<Query, Result<List<UserProfile>>>
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

      public async Task<Result<List<UserProfile>>> Handle(Query request, CancellationToken cancellationToken)
      {
        var profiles = new List<UserProfile>();

        switch (request.Predicate)
        {
          case "followers":
            profiles = await _context.UserFollowings.Where(x => x.Target.UserName == request.Username)
              .Select(u => u.Observer)
              .ProjectTo<UserProfile>(_mapper.ConfigurationProvider, new { currentUsername = _userAccessor.GetUsername() })
              .ToListAsync(cancellationToken);
            break;
          case "following":
            profiles = await _context.UserFollowings.Where(x => x.Observer.UserName == request.Username)
              .Select(u => u.Target)
              .ProjectTo<UserProfile>(_mapper.ConfigurationProvider, new { currentUsername = _userAccessor.GetUsername() })
              .ToListAsync(cancellationToken);
            break;
        }

        return Result<List<UserProfile>>.Success(profiles);
      }
    }
  }
}
