using Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Persistence
{
  public class DataContext : IdentityDbContext<AppUser>
  {
    public DataContext(DbContextOptions options) : base(options)
    {
    }

    // note: no need to add a DbSet for our users etc. Identity takes care of all that
    public DbSet<Activity> Activities { get; set; }
    public DbSet<ActivityAttendee> ActivityAttendees { get; set; }
    public DbSet<Photo> Photos { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<UserFollowing> UserFollowings { get; set; }

    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
    public DbSet<RecipeStep> RecipeSteps { get; set; }
    public DbSet<RecipeTag> RecipeTags { get; set; }
    public DbSet<RecipePhoto> RecipePhotos { get; set; }

    // note: overriding onModelCreating method to add additional configurations
    // will allow us to have access to our entity configurations
    protected override void OnModelCreating(ModelBuilder builder)
    {
      base.OnModelCreating(builder);

      // note: configuring ActivityAttendee entity
      // configuring primary key
      builder.Entity<ActivityAttendee>(x => x.HasKey(aa => new { aa.AppUserId, aa.ActivityId }));

      // configuring the entities themselves
      // For ActivityAttendees it's a many to many relationship so we configure both sides of the relationship
      builder.Entity<ActivityAttendee>()
        .HasOne(u => u.AppUser)
        .WithMany(a => a.Activities)
        .HasForeignKey(aa => aa.AppUserId);

      builder.Entity<ActivityAttendee>()
        .HasOne(u => u.Activity)
        .WithMany(a => a.Attendees)
        .HasForeignKey(aa => aa.ActivityId);

      // configuring cascade behavior to delete associated comments once activity is deleted
      builder.Entity<Comment>()
        .HasOne(a => a.Activity)
        .WithMany(c => c.Comments)
        .OnDelete(DeleteBehavior.Cascade);

      builder.Entity<UserFollowing>(b =>
      {
        b.HasKey(k => new {k.ObserverId, k.TargetId});

        b.HasOne(o => o.Observer)
          .WithMany(f => f.Followings)
          .HasForeignKey(o => o.ObserverId)
          .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(o => o.Target)
          .WithMany(f => f.Followers)
          .HasForeignKey(o => o.TargetId)
          .OnDelete(DeleteBehavior.Cascade);
      });

      builder.Entity<Recipe>(b =>
      {
        b.HasOne(r => r.Owner)
          .WithMany(u => u.Recipes)
          .HasForeignKey(r => r.OwnerId)
          .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(r => r.Ingredients)
          .WithOne(i => i.Recipe)
          .HasForeignKey(i => i.RecipeId)
          .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(r => r.Steps)
          .WithOne(s => s.Recipe)
          .HasForeignKey(s => s.RecipeId)
          .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(r => r.Tags)
          .WithOne(t => t.Recipe)
          .HasForeignKey(t => t.RecipeId)
          .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(r => r.Photos)
          .WithOne(p => p.Recipe)
          .HasForeignKey(p => p.RecipeId)
          .OnDelete(DeleteBehavior.Cascade);
      });

      builder.Entity<RecipeTag>()
        .HasIndex(t => new { t.RecipeId, t.TagName })
        .IsUnique();
    }
  }
}