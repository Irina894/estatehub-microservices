using AutoMapper;
using Deals.Bll.DTOs;
using Deals.Domain.Exceptions;
using Deals.Domain.Interfaces;
using Deals.Domain.Models;

namespace Deals.Bll.Services;

public interface IApplicationService
{
    Task<ApplicationDto> CreateAsync(CreateApplicationDto dto);
    Task<ApplicationDto> GetAsync(Guid id);
    Task<PagedResultDto<ApplicationDto>> GetPagedAsync(int page, int pageSize, string? status);
    Task<ApplicationDto> ChangeStatusAsync(Guid id, ChangeApplicationStatusDto dto);
    Task DeleteAsync(Guid id);

    Task<ViewingDto> ApproveAsync(Guid id, ApproveApplicationDto dto);
    Task<ContractDto> SignContractAsync(Guid id, SignContractDto dto);
    Task<ContractDto> GetContractAsync(Guid id);

    Task<ApplicationRealtorDto> AssignRealtorAsync(Guid id, AssignRealtorDto dto);
    Task<IReadOnlyList<ApplicationRealtorDto>> GetRealtorsAsync(Guid id);
    Task<IReadOnlyList<ViewingDto>> GetViewingsAsync(Guid id);
}

public class ApplicationService(IUnitOfWork uow, IMapper mapper) : IApplicationService
{
    // ---------- CRUD ----------

    public async Task<ApplicationDto> CreateAsync(CreateApplicationDto dto)
    {
        // рівень 2 валідації: бізнес-правила
        if (dto.ClientId == Guid.Empty) throw new DomainValidationException("ClientId is required.");
        if (dto.PropertyId == Guid.Empty) throw new DomainValidationException("PropertyId is required.");
        if (dto.PriceSnapshot <= 0) throw new DomainValidationException("PriceSnapshot must be positive.");

        var app = new Application
        {
            ClientId = dto.ClientId,
            PropertyId = dto.PropertyId,
            PriceSnapshot = dto.PriceSnapshot,
            PropertyTitleSnapshot = dto.PropertyTitleSnapshot.Trim(),
            Status = ApplicationStatuses.New,
            CreatedBy = null
        };
        await uow.Applications.AddAsync(app);
        return mapper.Map<ApplicationDto>(await GetEntityAsync(app.Id));
    }

    public async Task<ApplicationDto> GetAsync(Guid id) =>
        mapper.Map<ApplicationDto>(await GetEntityAsync(id));

    public async Task<PagedResultDto<ApplicationDto>> GetPagedAsync(int page, int pageSize, string? status)
    {
        if (page < 1) throw new DomainValidationException("Page must be >= 1.");
        if (pageSize is < 1 or > 100) throw new DomainValidationException("PageSize must be between 1 and 100.");
        if (status is not null && !ApplicationStatuses.IsKnown(status))
            throw new DomainValidationException($"Unknown status '{status}'.");

        var result = await uow.Applications.GetPagedAsync(page, pageSize, status);
        return new PagedResultDto<ApplicationDto>
        {
            Items = mapper.Map<List<ApplicationDto>>(result.Items),
            TotalCount = result.TotalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        var app = await GetEntityAsync(id);
        if (app.Status == ApplicationStatuses.Completed)
            throw new BusinessConflictException("Cannot delete a completed application."); // як у sp_SoftDeleteApplication
        if (!await uow.Applications.DeleteAsync(id))
            throw new NotFoundException($"Application {id} not found.");
    }

    // ---------- Зміна статусу (лише відхилення; approved/completed - через спеціальні операції) ----------

    public async Task<ApplicationDto> ChangeStatusAsync(Guid id, ChangeApplicationStatusDto dto)
    {
        await uow.ExecuteInTransactionAsync(async () =>
        {
            var app = await GetEntityAsync(id);

            if (!ApplicationStatuses.IsKnown(dto.Status))
                throw new DomainValidationException($"Unknown status '{dto.Status}'.");
            if (!ApplicationStatuses.CanTransition(app.Status, dto.Status))
                throw new BusinessConflictException($"Transition '{app.Status}' -> '{dto.Status}' is not allowed.");
            if (dto.Status is ApplicationStatuses.Approved or ApplicationStatuses.Completed)
                throw new BusinessConflictException(
                    "Use POST /approve to approve and POST /contract to complete an application.");

            await SetStatusAsync(app, dto.Status, dto.RowVersion);
            return true;
        });
        return await GetAsync(id);
    }

    // ---------- Транзакція 1: затвердження заявки + створення огляду (Applications + Realtors + Viewings) ----------

    public async Task<ViewingDto> ApproveAsync(Guid id, ApproveApplicationDto dto)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            var app = await GetEntityAsync(id);
            if (app.Status != ApplicationStatuses.New)
                throw new BusinessConflictException("Only a 'new' application can be approved.");

            if (dto.RealtorId == Guid.Empty) throw new DomainValidationException("RealtorId is required.");
            if (dto.ViewingDate.ToUniversalTime() <= DateTime.UtcNow)
                throw new DomainValidationException("ViewingDate must be in the future.");

            var realtor = await uow.Realtors.GetByIdAsync(dto.RealtorId)
                ?? throw new NotFoundException($"Realtor {dto.RealtorId} not found.");
            if (!realtor.IsActive)
                throw new BusinessConflictException("Realtor is not active.");

            await SetStatusAsync(app, ApplicationStatuses.Approved, dto.RowVersion);

            var viewing = new Viewing
            {
                ApplicationId = app.Id,
                RealtorId = realtor.Id,
                Date = dto.ViewingDate,
                Status = "scheduled"
            };
            await uow.Viewings.AddAsync(viewing);
            return mapper.Map<ViewingDto>(viewing);
        });
    }

    // ---------- Транзакція 2: підписання договору (Applications + Contracts) ----------

    public async Task<ContractDto> SignContractAsync(Guid id, SignContractDto dto)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            var app = await GetEntityAsync(id);
            if (app.Status != ApplicationStatuses.Approved)
                throw new BusinessConflictException("Contract can be signed only for an 'approved' application.");
            if (dto.FinalPrice <= 0)
                throw new DomainValidationException("FinalPrice must be positive.");
            if (await uow.Contracts.GetByApplicationAsync(id) is not null)
                throw new BusinessConflictException("A contract for this application already exists.");

            await SetStatusAsync(app, ApplicationStatuses.Completed, dto.RowVersion);

            var contract = new Contract
            {
                ApplicationId = app.Id,
                FinalPrice = dto.FinalPrice,
                SignedAt = DateTime.UtcNow
            };
            await uow.Contracts.AddAsync(contract);
            return mapper.Map<ContractDto>(contract);
        });
    }

    public async Task<ContractDto> GetContractAsync(Guid id)
    {
        await GetEntityAsync(id);
        var contract = await uow.Contracts.GetByApplicationAsync(id)
            ?? throw new NotFoundException($"Contract for application {id} not found.");
        return mapper.Map<ContractDto>(contract);
    }

    // ---------- M:N: рієлтори заявки ----------

    public async Task<ApplicationRealtorDto> AssignRealtorAsync(Guid id, AssignRealtorDto dto)
    {
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            var app = await GetEntityAsync(id);
            if (app.Status is ApplicationStatuses.Completed or ApplicationStatuses.Rejected)
                throw new BusinessConflictException("Realtors cannot be assigned to a closed application.");
            if (dto.RealtorId == Guid.Empty) throw new DomainValidationException("RealtorId is required.");
            if (!RealtorRoles.IsValid(dto.Role))
                throw new DomainValidationException("Role must be 'listing_agent' or 'buyer_agent'.");

            var realtor = await uow.Realtors.GetByIdAsync(dto.RealtorId)
                ?? throw new NotFoundException($"Realtor {dto.RealtorId} not found.");
            if (!realtor.IsActive) throw new BusinessConflictException("Realtor is not active.");
            if (await uow.ApplicationRealtors.ExistsAsync(app.Id, realtor.Id))
                throw new BusinessConflictException("Realtor is already assigned to this application.");

            var link = new ApplicationRealtor
            {
                ApplicationId = app.Id,
                RealtorId = realtor.Id,
                Role = dto.Role,
                AssignedAt = DateTime.UtcNow
            };
            await uow.ApplicationRealtors.AddAsync(link);
            return mapper.Map<ApplicationRealtorDto>(link);
        });
    }

    public async Task<IReadOnlyList<ApplicationRealtorDto>> GetRealtorsAsync(Guid id)
    {
        await GetEntityAsync(id);
        return mapper.Map<List<ApplicationRealtorDto>>(await uow.ApplicationRealtors.GetByApplicationAsync(id));
    }

    public async Task<IReadOnlyList<ViewingDto>> GetViewingsAsync(Guid id)
    {
        await GetEntityAsync(id);
        return mapper.Map<List<ViewingDto>>(await uow.Viewings.GetByApplicationAsync(id));
    }

    // ---------- helpers ----------

    private async Task<Application> GetEntityAsync(Guid id) =>
        await uow.Applications.GetByIdAsync(id)
        ?? throw new NotFoundException($"Application {id} not found.");

    /// <summary>Оновлює статус із перевіркою RowVersion; 0 змінених рядків = хтось змінив заявку раніше.</summary>
    private async Task SetStatusAsync(Application app, string newStatus, byte[] rowVersion)
    {
        if (!ApplicationStatuses.CanTransition(app.Status, newStatus))
            throw new BusinessConflictException($"Transition '{app.Status}' -> '{newStatus}' is not allowed.");

        app.Status = newStatus;
        app.RowVersion = rowVersion;
        if (!await uow.Applications.UpdateAsync(app))
            throw new BusinessConflictException(
                "The application was modified by another user. Reload it and retry (stale RowVersion).");
    }
}
