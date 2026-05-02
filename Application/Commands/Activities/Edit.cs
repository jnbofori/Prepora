using Application.Validators;
using Application.Core;
using AutoMapper;
using Domain;
using Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Application.Commands.Activities
{
  public class Edit
  {
    public class Command : IRequest<Result<Unit>>
    {
      public Activity activity { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
      public CommandValidator()
      {
        RuleFor(x => x.activity).SetValidator(new ActivityValidator());
      }
    }

    public class Handler : IRequestHandler<Command, Result<Unit>>
    {
      private readonly IActivityRepository _activities;
      private readonly IUnitOfWork _unitOfWork;
      private readonly IMapper _mapper;

      public Handler(IActivityRepository activities, IUnitOfWork unitOfWork, IMapper mapper)
      {
        _activities = activities;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
      }

      public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
      {
        var activity = await _activities.GetByIdAsync(request.activity.Id, cancellationToken);

        if (activity == null) return null;

        _mapper.Map(request.activity, activity);

        var result = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;

        if (!result) return Result<Unit>.Failure("Failed to edit activity");

        return Result<Unit>.Success(Unit.Value);
      }
    }
  }
}
