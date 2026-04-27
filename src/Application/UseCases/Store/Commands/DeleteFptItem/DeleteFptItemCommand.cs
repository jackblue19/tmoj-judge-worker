using MediatR;
using System;

namespace Application.UseCases.Store.Commands.DeleteFptItem;

public record DeleteFptItemCommand(Guid ItemId) : IRequest<bool>;
