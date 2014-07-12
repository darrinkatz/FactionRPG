using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

public class FactionRPG : DbContext
{
    public DbSet<Asset> Assets { get; set; }
    public DbSet<Faction> Factions { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<Infiltration> Infiltrations { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Profile> Profiles { get; set; }
}