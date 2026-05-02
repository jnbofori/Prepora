using Application.DTOs.Activities;
using Application.DTOs.Comments;
using Application.DTOs.Profiles;
using Application.DTOs.Recipes;
using AutoMapper;
using Domain;
using UserProfile = Application.DTOs.Profiles.Profile;

namespace Application.Core
{
  public class MappingProfiles : AutoMapper.Profile
  {
    public MappingProfiles()
    {
      string currentUsername = null;
      CreateMap<Activity, Activity>();
      CreateMap<Activity, ActivityDto>()
        .ForMember(
          destination => destination.HostUsername,
          options => options.MapFrom(source => source.Attendees.FirstOrDefault(x => x.IsHost).AppUser.UserName)
        );
      CreateMap<ActivityAttendee, AttendeeDto>()
        .ForMember(
          destination => destination.DisplayName,
          options => options.MapFrom(source => source.AppUser.DisplayName)
        )
        .ForMember(
          destination => destination.Username,
          options => options.MapFrom(source => source.AppUser.UserName)
        )
        .ForMember(
          destination => destination.Bio,
          options => options.MapFrom(source => source.AppUser.Bio)
        )
        .ForMember(d => d.Image, o => o.MapFrom(s => s.AppUser.Photos.FirstOrDefault(x => x.IsMain).Url))
        .ForMember(d => d.FollowersCount, o => o.MapFrom(s => s.AppUser.Followers.Count))
        .ForMember(d => d.FollowingCount, o => o.MapFrom(s => s.AppUser.Followings.Count))
        .ForMember(d => d.Following, o => o.MapFrom(s => s.AppUser.Followers.Any(x => x.Observer.UserName == currentUsername)));

      CreateMap<AppUser, UserProfile>()
        .ForMember(d => d.Image, o => o.MapFrom(s => s.Photos.FirstOrDefault(x => x.IsMain).Url))
        .ForMember(d => d.FollowersCount, o => o.MapFrom(s => s.Followers.Count))
        .ForMember(d => d.FollowingCount, o => o.MapFrom(s => s.Followings.Count))
        .ForMember(d => d.Following, o => o.MapFrom(s => s.Followers.Any(x => x.Observer.UserName == currentUsername)));

      CreateMap<Comment, CommentDto>()
        .ForMember(d => d.Username, o => o.MapFrom(s => s.Author.UserName))
        .ForMember(
          d => d.DisplayName,
          o => o.MapFrom(s => s.Author.DisplayName)
        )
        .ForMember(d => d.Image, o => o.MapFrom(s => s.Author.Photos.FirstOrDefault(x => x.IsMain).Url));

      CreateMap<ActivityAttendee, UserActivityDto>()
        .ForMember(d => d.Id, o => o.MapFrom(s => s.Activity.Id))
        .ForMember(d => d.Date, o => o.MapFrom(s => s.Activity.Date))
        .ForMember(d => d.Title, o => o.MapFrom(s => s.Activity.Title))
        .ForMember(d => d.Category, o => o.MapFrom(s => s.Activity.Category))
        .ForMember(d => d.HostUsername, o => o.MapFrom(s => s.Activity.Attendees.FirstOrDefault(x => x.IsHost).AppUser.UserName));

      CreateMap<RecipeIngredient, RecipeIngredientDto>();
      CreateMap<RecipeStep, RecipeStepDto>();
      CreateMap<RecipePhoto, RecipePhotoDto>();

      CreateMap<Recipe, RecipeDto>()
        .ForMember(d => d.Ingredients, o => o.MapFrom(s => s.Ingredients.OrderBy(i => i.SortOrder)))
        .ForMember(d => d.Steps, o => o.MapFrom(s => s.Steps.OrderBy(x => x.SortOrder)))
        .ForMember(d => d.Tags, o => o.MapFrom(s => s.Tags.OrderBy(t => t.TagName).Select(t => t.TagName).ToList()))
        .ForMember(d => d.Photos, o => o.MapFrom(s => s.Photos.OrderBy(p => p.SortOrder)));

      CreateMap<Recipe, RecipeListDto>()
        .ForMember(d => d.Tags, o => o.MapFrom(s => s.Tags.OrderBy(t => t.TagName).Select(t => t.TagName).ToList()))
        .ForMember(d => d.CoverImageUrl, o => o.MapFrom(s =>
          s.Photos.Where(p => p.IsCover).OrderBy(p => p.SortOrder).Select(p => p.Url).FirstOrDefault()
          ?? s.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).FirstOrDefault()));
    }
  }
}
