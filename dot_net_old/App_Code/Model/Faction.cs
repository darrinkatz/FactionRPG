using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Web.UI.WebControls;

public class Faction
{
    public long Id { get; set; }
    public virtual Game Game { get; set; }
    public string PlayerEmail { get; set; }
    public string PlayerUserName { get; set; }
    public string Name { get; set; }
    public bool Committed { get; set; }
    public string Notes { get; set; }
    public string HexColour { get; set; }

    public ICollection<Asset> Assets { get; set; }
    public ICollection<Profile> Profiles { get; set; }
    public ICollection<Order> Orders { get; set; }

    public Faction()
    {
        Assets = new List<Asset>();
        Profiles = new List<Profile>();
        Orders = new List<Order>();
        HexColour = "00FF00";
    }
}

public static class FactionExtensions
{
    public static int SpentBP(this Faction faction)
    {
        var result = 0;

        foreach (var asset in Service.GetAssetsOfFaction(faction.Id))
        {
            result += asset.BP();
        }

        return result;
    }

    public static List<Asset> OneTargetPerAction(this Faction faction, int turn, long perspective)
    {
        faction = Service.GetFaction(faction.Id);
        var game = Service.GetGameOfFaction(faction);

        var allVisibleAssets = Service.GetAssetsOfFactionFromPerspective(game, perspective, turn, faction.Id);

        var oneTargetIdPerAction = new List<long>();

        foreach (var visibleAsset in allVisibleAssets)
        {
            var allAssetIdsTargetingThis = Service.GetAssetIdsTargetingAsset(visibleAsset.Id, perspective);

            foreach (var assetId in allAssetIdsTargetingThis)
            {
                if (!oneTargetIdPerAction.Contains(assetId))
                {
                    bool actionAlreadyIncluded = false;

                    var asset = Service.GetAsset(assetId);

                    foreach (var collaboratorId in Service.GetCollaboratingAssetIds(asset, perspective))
                    {
                        if (oneTargetIdPerAction.Contains(collaboratorId))
                        {
                            actionAlreadyIncluded = true;
                            break;
                        }
                    }

                    if (!actionAlreadyIncluded)
                    {
                        oneTargetIdPerAction.Add(assetId);
                    }
                }
            }
        }

        return Service.GetAssets(oneTargetIdPerAction);
    }

    public static List<Faction> OptionsForShotInTheDark(this Faction faction, int turn)
    {
        faction = Service.GetFaction(faction.Id);

        var possibleFactionIds = Service.GetOtherFactions(faction).Select(f => f.Id).ToList();

        var invalidFactionIds = new List<long>();

        var visibleAssetIds = Service.GetAssetsFromPerspective(Service.GetGameOfFaction(faction), faction.Id, turn).Select(x => x.Id).ToList();

        foreach (var visibleAssetId in visibleAssetIds)
        {
            var visibleAsset = Service.GetAsset(visibleAssetId);
            var factionOfVisibleAsset = Service.GetFactionOfAsset(visibleAsset);
            if (possibleFactionIds.Contains(factionOfVisibleAsset.Id))
            {
                invalidFactionIds.Add(factionOfVisibleAsset.Id);
            }
        }

        return Service.GetFactions(possibleFactionIds).Where(f => !invalidFactionIds.Contains(f.Id)).ToList();
    }

    public static List<Asset> OneAssetPerAction(this Faction faction, int turn, long perspective)
    {
        faction = Service.GetFaction(faction.Id);
        var game = Service.GetGameOfFaction(faction);

        var allAssetIds = Service.GetAssetsFromPerspective(game, perspective, turn).Where(a => Service.GetFactionOfAsset(a).Id == faction.Id).Select(a => a.Id).ToList();

        var oneAssetIdPerAction = new List<long>();
        foreach (var assetId in allAssetIds)
        {
            if (!oneAssetIdPerAction.Contains(assetId))
            {
                bool actionAlreadyIncluded = false;

                var asset = Service.GetAsset(assetId);

                foreach (var collaboratorId in Service.GetCollaboratingAssetIds(asset, perspective))
                {
                    if (oneAssetIdPerAction.Contains(collaboratorId))
                    {
                        actionAlreadyIncluded = true;
                        break;
                    }
                }

                if (!actionAlreadyIncluded)
                {
                    oneAssetIdPerAction.Add(assetId);
                }
            }
        }

        return Service.GetAssets(oneAssetIdPerAction);
    }

    public static List<ListItem> AssetsByFaction(this Faction faction, List<Asset> assets, long perspective)
    {
        var result = new List<ListItem>();

        faction = Service.GetFaction(faction.Id);
        var game = Service.GetGameOfFaction(faction);
        var factions = Service.GetFactionsInGame(game.Id);

        foreach (var f in factions)
        {
            var thisFactionsAssetIds = Service.GetAssetsOfFaction(f.Id).Select(a => a.Id).ToList();

            var liList = new List<ListItem>();

            foreach (var asset in assets)
            {
                if (thisFactionsAssetIds.Contains(asset.Id))
                {
                    liList.Add(
                        new ListItem(
                            asset.Render(perspective),
                            asset.Id.ToString()
                            ));
                }
            }

            if (liList.Count > 0)
            {
                result.Add(
                    new ListItem(
                        string.Format("===== {0} =====", f.Name),
                        "0"
                        ));

                result.AddRange(liList.ToArray());
            }
        }

        return result;
    }
}

public class FactionNameComparer : IComparer<Faction>
{
    int IComparer<Faction>.Compare(Faction x, Faction y)
    {
        return x.Name.CompareTo(y.Name);
    }
}