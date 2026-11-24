using AutoMapper;
using Deals.Bll.DTOs;
using Deals.Domain.Models;

namespace Deals.Bll.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Application, ApplicationDto>();
        CreateMap<Viewing, ViewingDto>();
        CreateMap<Contract, ContractDto>();
        CreateMap<ApplicationRealtor, ApplicationRealtorDto>();

        CreateMap<Realtor, RealtorDto>();
        CreateMap<CreateRealtorDto, Realtor>();
        CreateMap<UpdateRealtorDto, Realtor>();
    }
}
