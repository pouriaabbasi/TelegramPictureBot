using TelegramPhotoBot.Application.Interfaces;
using TelegramPhotoBot.Application.Interfaces.Repositories;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.ValueObjects;
using FileInfo = TelegramPhotoBot.Domain.ValueObjects.FileInfo;

namespace TelegramPhotoBot.Application.Services;

/// <summary>
/// Service for managing content creator models
/// </summary>
public class ModelService : IModelService
{
    private readonly IModelRepository _modelRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthorizationService _authorizationService;

    public ModelService(
        IModelRepository modelRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IAuthorizationService authorizationService)
    {
        _modelRepository = modelRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _authorizationService = authorizationService;
    }

    public async Task<Model> RegisterModelAsync(Guid userId, string displayName, string? bio, CancellationToken cancellationToken = default)
    {
        // Check if user exists
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        // Check if user already has a model
        var existingModel = await _modelRepository.GetByUserIdAsync(userId, cancellationToken);
        if (existingModel != null)
        {
            throw new InvalidOperationException("User already has a model registered");
        }

        // Create new model
        var model = new Model(userId, displayName, bio);
        
        await _modelRepository.AddAsync(model, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return model;
    }

    public async Task<Model?> GetModelByIdAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        return await _modelRepository.GetByIdAsync(modelId, cancellationToken);
    }

    public async Task<Model?> GetModelByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _modelRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    public async Task<IEnumerable<Model>> GetApprovedModelsAsync(CancellationToken cancellationToken = default)
    {
        return await _modelRepository.GetApprovedModelsAsync(0, 100, cancellationToken); // Get first 100 models
    }

    public async Task<IEnumerable<Model>> GetPendingApprovalModelsAsync(CancellationToken cancellationToken = default)
    {
        return await _modelRepository.GetPendingModelsAsync(cancellationToken);
    }

    public async Task<Model> ApproveModelAsync(Guid modelId, Guid approvedByAdminId, CancellationToken cancellationToken = default)
    {
        // Ensure the approver is an admin
        await _authorizationService.EnsureIsAdminAsync(approvedByAdminId, cancellationToken);

        var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken);
        if (model == null)
        {
            throw new InvalidOperationException("Model not found");
        }

        model.Approve(approvedByAdminId);
        
        // Promote user to Model role
        var user = await _userRepository.GetByIdAsync(model.UserId, cancellationToken);
        if (user != null)
        {
            user.PromoteToModel(modelId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return model;
    }

    public async Task<Model> RejectModelAsync(Guid modelId, Guid rejectedByAdminId, string reason, CancellationToken cancellationToken = default)
    {
        // Ensure the rejecter is an admin
        await _authorizationService.EnsureIsAdminAsync(rejectedByAdminId, cancellationToken);

        var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken);
        if (model == null)
        {
            throw new InvalidOperationException("Model not found");
        }

        model.Reject(rejectedByAdminId, reason);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return model;
    }

    public async Task<Model> UpdateProfileAsync(Guid modelId, string displayName, string? bio, CancellationToken cancellationToken = default)
    {
        var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken);
        if (model == null)
        {
            throw new InvalidOperationException("Model not found");
        }

        model.UpdateProfile(displayName, bio);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return model;
    }

    public async Task<Model> SetDemoImageAsync(Guid modelId, FileInfo demoImage, CancellationToken cancellationToken = default)
    {
        var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken);
        if (model == null)
        {
            throw new InvalidOperationException("Model not found");
        }

        model.SetDemoImage(demoImage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return model;
    }

    public async Task<Model> RemoveDemoImageAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken);
        if (model == null)
        {
            throw new InvalidOperationException("Model not found");
        }

        model.RemoveDemoImage();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return model;
    }

    public async Task<Model> SetSubscriptionPricingAsync(Guid modelId, TelegramStars price, int durationDays, CancellationToken cancellationToken = default)
    {
        var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken);
        if (model == null)
        {
            throw new InvalidOperationException("Model not found");
        }

        model.SetSubscriptionPricing(price, durationDays);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return model;
    }

    public async Task<Model> SuspendModelAsync(Guid modelId, string reason, CancellationToken cancellationToken = default)
    {
        var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken);
        if (model == null)
        {
            throw new InvalidOperationException("Model not found");
        }

        model.Suspend(reason);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return model;
    }

    public async Task<Model> ReactivateModelAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken);
        if (model == null)
        {
            throw new InvalidOperationException("Model not found");
        }

        model.Reactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return model;
    }
}

