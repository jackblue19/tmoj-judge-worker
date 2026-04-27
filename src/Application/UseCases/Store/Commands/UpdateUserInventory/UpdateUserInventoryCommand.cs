using MediatR;
using System;

namespace Application.UseCases.Store.Commands.UpdateUserInventory;

public record UpdateUserInventoryCommand(Guid InventoryId, bool IsEquipped) : IRequest<bool>;
