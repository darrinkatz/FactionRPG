using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class Profile
{
    public long Id { get; set; }
    public virtual Faction Faction { get; set; }
    public Asset Asset { get; set; }
    public long SenderFactionId { get; set; }

    public Profile()
    {

    }
}

public static class ProfileExtensions
{
    public static string Render(this Profile profile, long perspective)
    {
        return string.Format("You gave {0} a profile on {1}.", Service.GetFactionOfProfile(profile).Name, Service.GetAssetOfProfile(profile.Id).Render(perspective));
    }
}