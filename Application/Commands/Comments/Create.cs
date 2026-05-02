using Application.DTOs.Comments;
using Application.Core;
using Application.Interfaces;
using AutoMapper;
using Domain;
using Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Commands.Comments
{
  public class Create
  {
    public class Command : IRequest<Result<CommentDto>>
    {
      public string Body { get; set; }
      public Guid ActivityId { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
      public CommandValidator()
      {
        RuleFor(x => x.Body).NotEmpty();
      }
    }

    public class Handler : IRequestHandler<Command, Result<CommentDto>>
    {
      private readonly IActivityRepository _activities;
      private readonly IUserRepository _users;
      private readonly IUnitOfWork _unitOfWork;
      private readonly IUserAccessor _userAccessor;
      private readonly IMapper _mapper;

      public Handler(
        IActivityRepository activities,
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IUserAccessor userAccessor)
      {
        _activities = activities;
        _users = users;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userAccessor = userAccessor;
      }

      public async Task<Result<CommentDto>> Handle(Command request, CancellationToken cancellationToken)
      {
        var activity = await _activities.GetByIdAsync(request.ActivityId, cancellationToken);
        if (activity == null) return null;

        var user = await _users.GetByUsernameWithPhotosAsync(_userAccessor.GetUsername(), cancellationToken);

        var comment = new Comment
        {
          Author = user,
          Activity = activity,
          Body = request.Body
        };

        activity.Comments.Add(comment);

        var success = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;

        if (success) return Result<CommentDto>.Success(_mapper.Map<CommentDto>(comment));

        return Result<CommentDto>.Failure("Failed to add comment");
      }
    }
  }
}
