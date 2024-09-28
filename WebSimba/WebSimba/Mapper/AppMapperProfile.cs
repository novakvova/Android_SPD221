using AutoMapper;
using WebSimba.Data.Entities;
using WebSimba.Models.Category;

namespace WebSimba.Mapper
{
    public class AppMapperProfile : Profile
    {
        public AppMapperProfile()
        {
            //Початок
            CreateMap<CategoryEntity, CategoryItemModel>()
                .ForMember(x=>x.ImagePath, opt=>opt.MapFrom(x=>
                    string.IsNullOrEmpty(x.Image) ? "/images/noimage.jpg" : $"/images/{x.Image}"));
        }
    }
}
