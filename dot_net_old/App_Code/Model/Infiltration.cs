using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class Infiltration
{
    public long Id { get; set; }
    public virtual Asset Asset { get; set; }
    public long FactionId { get; set; }
    public int ProfileCount { get; set; }

	public Infiltration()
	{
        
	}
}