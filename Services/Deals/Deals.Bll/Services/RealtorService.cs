using AutoMapper;
using Deals.Bll.DTOs;
using Deals.Domain.Exceptions;
using Deals.Domain.Interfaces;
using Deals.Domain.Models;

namespace Deals.Bll.Services;

public interface IRealtorService
{
    Task<RealtorDto> CreateAsync(CreateRealtorDto dto);
    Task<RealtorDto> GetAsync(Guid id);
    Task<IReadOnlyList<RealtorDto>> GetAllAsync();
    Task<RealtorDto> UpdateAsync(Guid id, UpdateRealtorDto dto);
    Task DeactivateAsync(Guid id);
}

public class RealtorService(IUnitOfWork uow, IMapper mapper) : IRealtorService
{
    public async Task<RealtorDto> CreateAsync(CreateRealtorDto dto)
    {
        var email = dto.Email.Trim();
        if (await uow.Realtors.GetByEmailAsync(email) is not null)
            throw new BusinessConflictException($"Realtor with email '{email}' already exists.");

        var realtor = mapper.Map<Realtor>(dto);
        realtor.Email = email;
        realtor.IsActive = true;
        await uow.Realtors.AddAsync(realtor);
        return mapper.Map<RealtorDto>(realtor);
    }

    public async Task<RealtorDto> GetAsync(Guid id) =>
        mapper.Map<RealtorDto>(await uow.Realtors.GetByIdAsync(id)
            ?? throw new NotFoundException($"Realtor {id} not found."));

    public async Task<IReadOnlyList<RealtorDto>> GetAllAsync() =>
        mapper.Map<List<RealtorDto>>(await uow.Realtors.GetAllAsync());

    public async Task<RealtorDto> UpdateAsync(Guid id, UpdateRealtorDto dto)
    {
        var realtor = await uow.Realtors.GetByIdAsync(id)
            ?? throw new NotFoundException($"Realtor {id} not found.");

        var email = dto.Email.Trim();
        var other = await uow.Realtors.GetByEmailAsync(email);
        if (other is not null && other.Id != id)
            throw new BusinessConflictException($"Realtor with email '{email}' already exists.");

        mapper.Map(dto, realtor);
        realtor.Email = email;
        await uow.Realtors.UpdateAsync(realtor);
        return mapper.Map<RealtorDto>(realtor);
    }

    public async Task DeactivateAsync(Guid id)
    {
        if (!await uow.Realtors.DeleteAsync(id))
            throw new NotFoundException($"Realtor {id} not found.");
    }
}
