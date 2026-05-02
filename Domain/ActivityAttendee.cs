using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// note: by convention we could have specified a foreign key/entity for each entity
// and added a migration to create the join table 
// but this wouldn't allow us to add our own properties and stuff (like 'isHost')
namespace Domain
{
  public class ActivityAttendee
  {
    public string AppUserId { get; set; }

    public AppUser AppUser { get; set; }

    public Guid ActivityId { get; set; }

    public Activity Activity { get; set; }

    public bool IsHost { get; set; }
  }
}