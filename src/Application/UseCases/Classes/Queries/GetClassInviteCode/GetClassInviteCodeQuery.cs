using Application.UseCases.Classes.Dtos;
using MediatR;

namespace Application.UseCases.Classes.Queries.GetClassInviteCode;

public record GetClassInviteCodeQuery(Guid ClassSemesterId) : IRequest<InviteCodeStatusDto>;
