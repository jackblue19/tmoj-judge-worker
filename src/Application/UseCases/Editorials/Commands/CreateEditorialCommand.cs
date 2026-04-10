
using MediatR;

namespace Application.UseCases.Editorials.Commands;

public class CreateEditorialCommand : IRequest<Guid>
{
    public Guid ProblemId { get; set; }
    public Guid StorageId { get; set; }
}

