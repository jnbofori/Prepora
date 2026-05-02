using Application.Core;
using Domain.Repositories;
using MediatR;

namespace Application.Commands.Activities
{
  public class Delete
  {
    public class Command : IRequest<Result<Unit>>
    {
      public Guid Id { get; set; }
    }

    public class Handler : IRequestHandler<Command, Result<Unit>>
    {
      private readonly IActivityRepository _activities;
      private readonly IUnitOfWork _unitOfWork;

      public Handler(IActivityRepository activities, IUnitOfWork unitOfWork)
      {
        _activities = activities;
        _unitOfWork = unitOfWork;
      }

      public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
      {
        var activity = await _activities.GetByIdAsync(request.Id, cancellationToken);

        if (activity == null) return null;

        _activities.Remove(activity);

        var result = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;

        if (!result) return Result<Unit>.Failure("Failed to delete");
        return Result<Unit>.Success(Unit.Value);
      }
    }
  }
}
