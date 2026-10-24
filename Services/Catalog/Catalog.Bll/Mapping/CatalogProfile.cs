using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Mapping;

public class CatalogProfile : Profile
{
    public CatalogProfile()
    {
        CreateMap<Category, CategoryDto>();
        CreateMap<Category, CategoryWithPropertiesDto>();
        CreateMap<Amenity, AmenityDto>();

        CreateMap<PropertyLocation, LocationDto>();
        CreateMap<PropertyImage, ImageDto>();

        // Category.Name -> CategoryName (flattening), enum -> нижній регістр
        CreateMap<Property, PropertyDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString().ToLower()));

        CreateMap<Property, PropertyDetailsDto>()
            .IncludeBase<Property, PropertyDto>()
            .ForMember(d => d.Images, o => o.MapFrom(s => s.Images.OrderBy(i => i.SortOrder)))
            .ForMember(d => d.Amenities, o => o.MapFrom(s => s.PropertyAmenities.Select(pa => pa.Amenity)));
    }
}
