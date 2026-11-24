using AutoMapper;
using Reviews.Application.Common.Models;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Mapping;

public sealed class ReviewProfile : Profile
{
    public ReviewProfile()
    {
        CreateMap<Author, AuthorDto>();
        CreateMap<Criteria, CriteriaDto>();
        CreateMap<Comment, CommentDto>();
        CreateMap<Review, ReviewDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id.ToString()));
    }
}
