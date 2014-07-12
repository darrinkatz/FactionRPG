using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class Order
{
    public long Id { get; set; }
    public virtual Faction Perspective { get; set; }

    public long AssetId { get; set; }
    public string Action { get; set; }
    public bool GoingPublic { get; set; }
    public string InfuseWith { get; set; }
    public bool IsSentinel { get; set; }
    public long ShotInTheDarkFactionId { get; set; }
    public long TargetId { get; set; }
    public int Turn { get; set; }
    public string PropagateWith { get; set; }
    
    public Order()
	{
        Action = string.Empty;
        InfuseWith = string.Empty;
        PropagateWith = string.Empty;
	}

    public enum Actions
    {
        Damage,
        Enhance,
        Infiltrate,
        Shroud
    }
}