<%@ WebService Language="C#" Class="GameService" %>

using System;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Web.Security;
using Jayrock.Json.Conversion;

[System.Web.Script.Services.ScriptService]
[WebService(Namespace = "http://www.factionrpg.com/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class GameService : System.Web.Services.WebService
{
    [WebMethod]
    public string UpdateAssets(string guid, long actorId, long targetId, string action)
    {
        var game = Service.GetGame(guid);

        long factionIdOfUser = GetFactionIdOfUser(game);

        if (factionIdOfUser > 0)
        {
            var perspective = new Perspective();
            perspective.Load(factionIdOfUser, game, game.CurrentTurn);
            perspective.ChangeOrder(factionIdOfUser, actorId, targetId, action); 
            return JsonConvert.ExportToString(
                new PerspectiveManager(
                    perspective
                ).UpdateAssets()
            );
        }
        else
        {
            return string.Empty;
        }
    }

    [WebMethod]
    public string GetGame(string guid, string turnString)
    {
        var game = Service.GetGame(guid);

        long factionIdOfUser = GetFactionIdOfUser(game);

        if (factionIdOfUser > 0)
        {
            int turn;
            
            if (!int.TryParse(turnString, out turn) || turn > game.CurrentTurn || turn < 1)
            {
                turn = game.CurrentTurn;
            }
            
            var perspective = new Perspective();
            perspective.Load(factionIdOfUser, game, turn);
            return JsonConvert.ExportToString(
                new PerspectiveManager(
                    perspective
                ).UpdateAssets()
            );
        }
        else
        {
            return string.Empty;
        }
    }

    private static long GetFactionIdOfUser(Game game)
    {
        long factionIdOfUser = 0;
        var factionsInGame = Service.GetFactionsInGame(game.Id);
        var factionsOfUser = Service.GetFactionsOfUser(Membership.GetUser().Email);

        foreach (var faction in factionsInGame)
        {
            if (factionsOfUser.Exists(f => f.Id == faction.Id))
            {
                factionIdOfUser = faction.Id;
            }
        }
        return factionIdOfUser;
    }
}