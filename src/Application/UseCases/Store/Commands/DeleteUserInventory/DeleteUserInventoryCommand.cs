using MediatR;
using System;

namespace Application.UseCases.Store.Commands.DeleteUserInventory;

public record DeleteUserInventoryCommand(Guid InventoryId) : IRequest<bool>;
