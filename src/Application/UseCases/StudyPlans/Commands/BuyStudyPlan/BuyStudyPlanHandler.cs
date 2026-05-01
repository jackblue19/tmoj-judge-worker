using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.StudyPlans.Commands.BuyStudyPlan;

public class BuyStudyPlanHandler : IRequestHandler<BuyStudyPlanCommand, Unit>
{
    private readonly IStudyPlanRepository _studyRepo;
    private readonly IWalletRepository _walletRepo;
    private readonly IUserStudyPlanPurchaseRepository _purchaseRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<BuyStudyPlanHandler> _logger;

    public BuyStudyPlanHandler(
        IStudyPlanRepository studyRepo,
        IWalletRepository walletRepo,
        IUserStudyPlanPurchaseRepository purchaseRepo,
        ICurrentUserService currentUser,
        ILogger<BuyStudyPlanHandler> logger)
    {
        _studyRepo = studyRepo;
        _walletRepo = walletRepo;
        _purchaseRepo = purchaseRepo;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Unit> Handle(BuyStudyPlanCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        _logger.LogInformation("BUY START | User={UserId} Plan={PlanId}", userId, request.StudyPlanId);

        var plan = await _studyRepo.GetByIdAsync(request.StudyPlanId)
            ?? throw new Exception("Study plan not found");

        // FREE PLAN
        if (!plan.IsPaid)
        {
            _logger.LogInformation("FREE PLAN → GRANT ACCESS");
            await GrantAccess(userId, plan.Id);
            return Unit.Value;
        }

        var exists = await _purchaseRepo.ExistsAsync(userId, plan.Id);
        if (exists)
        {
            _logger.LogWarning("Already purchased | User={UserId} Plan={PlanId}", userId, plan.Id);
            return Unit.Value;
        }

        var wallet = await _walletRepo.GetByUserIdAsync(userId)
            ?? throw new Exception("Wallet not found");

        _logger.LogInformation("Wallet before: {Balance}", wallet.Balance);

        if (wallet.Balance < plan.Price)
            throw new Exception("Not enough coins");

        wallet.Balance -= plan.Price;

        await _walletRepo.UpdateAsync(wallet);

        await _walletRepo.AddTransactionAsync(new WalletTransaction
        {
            TransactionId = Guid.NewGuid(),
            WalletId = wallet.WalletId,
            Type = "withdraw",
            Direction = "out",
            Amount = plan.Price,
            SourceType = "study_plan",
            SourceId = plan.Id,
            Status = "completed",
            CreatedAt = DateTime.UtcNow
        });

        _logger.LogInformation("Wallet deducted → new balance: {Balance}", wallet.Balance);

        try
        {
            await GrantAccess(userId, plan.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FAILED GRANT ACCESS");
            throw;
        }

        return Unit.Value;
    }

    private async Task GrantAccess(Guid userId, Guid studyPlanId)
    {
        _logger.LogInformation("GRANT ACCESS START");



        var entity = new UserStudyPlanPurchase
        {
            UserId = userId,
            StudyPlanId = studyPlanId,
            PurchasedAt = DateTime.UtcNow
        };

        _logger.LogInformation("INSERT PURCHASE | {@Entity}", entity);

        await _purchaseRepo.AddAsync(entity);

        try
        {
            await _purchaseRepo.SaveChangesAsync();
            _logger.LogInformation("SAVE SUCCESS");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SAVE FAILED - PURCHASE");

            throw new Exception(
                $"BUY FAILED: {ex.InnerException?.Message ?? ex.Message}",
                ex
            );
        }
    }
}