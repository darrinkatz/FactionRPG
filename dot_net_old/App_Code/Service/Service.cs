using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;

public class Service
{
    private static readonly object _dictionaryCacheLock = new object();
    private static readonly Dictionary<string, object> _cacheKeyLocks;

    static Service()
    {
        _cacheKeyLocks = new Dictionary<string, object>();
    }

    #region caching methods

    private static object GetLockObject(string key)
    {
        Object keyLock;
        bool doesKeyExist = _cacheKeyLocks.TryGetValue(key, out keyLock);

        if (!doesKeyExist)
        {
            lock (_dictionaryCacheLock)
            {
                doesKeyExist = _cacheKeyLocks.TryGetValue(key, out keyLock);

                if (!doesKeyExist)
                {
                    keyLock = new object();
                    _cacheKeyLocks.Add(key, keyLock);
                }
            }
        }

        return keyLock;
    }

    private static T Get<T>(string key, Func<T> operation)
    {
        Cache cache = HttpContext.Current.Cache;

        if (cache.Get(key) == null)
        {
            lock (GetLockObject(key))
            {
                if (cache[key] == null)
                {
                    var objectToCache = operation();

                    if (objectToCache != null)
                    {
                        cache.Insert(key, objectToCache, null, DateTime.Now.AddHours(1), TimeSpan.Zero);
                    }
                    else
                    {
                        return default(T);
                    }
                }
            }
        }

        return (T)cache[key];
    }

    private static T Get<T>(string key)
    {
        return (T)HttpContext.Current.Cache[key];
    }

    private static void ClearCache(string key)
    {
        Cache cache = HttpContext.Current.Cache;

        if (cache[key] != null)
        {
            cache.Remove(key);
        }
    }

    #endregion

    public void SavePerspectiveToDB()
    {
        // TODO: for each faction in game,
        // save assets to DB
        // save orders to DB
    }

    public static string GetHistory(Game game, int turn, long perspective)
    {
        return Service.Get(
            string.Format("GetHistory_{0}_{1}_{2}", game.Guid, turn, perspective),
            () =>
        {
            var result = string.Empty;

            var factionsInGame = Service.GetFactionsInGame(game.Id);
            factionsInGame.Sort(new FactionNameComparer());

            factionsInGame.RemoveAll(x => x.Id == perspective);
            factionsInGame.Insert(0, Service.GetFaction(perspective));

            foreach (var faction in factionsInGame)
            {
                result += string.Format("<fieldset><legend>{0} ({1})</legend>Actions Taken by {2}:", faction.Name, faction.PlayerUserName, faction.Name);

                var assets = faction.OneAssetPerAction(turn, perspective);

                if (assets.Count == 0)
                {
                    result += "<ul><li>No assets belonging to this faction were visible to you.</li></ul>";
                }
                else
                {
                    result += "<ul>";
                    foreach (var asset in assets)
                    {
                        result += string.Format("<li>{0}</li>", asset.ResultOfAction(perspective));
                    }
                    result += "</ul>";
                }

                result += string.Format("Actions Targeting {0}:", faction.Name);

                var targets = faction.OneTargetPerAction(turn, perspective);

                if (targets.Count == 0)
                {
                    result += "<ul><li>No assets taking actions against this faction were visible to you.</li></ul>";
                }
                else
                {
                    result += "<ul>";
                    foreach (var target in targets)
                    {
                        result += string.Format("<li>{0}</li>", target.ResultOfAction(perspective));
                    }
                    result += "</ul>";
                }

                result += "</fieldset>";
            }

            return result;
        });
    }

    public static void IncrementGameTurn(long gameId)
    {
        using (var context = new FactionRPG())
        {
            context.Games.SingleOrDefault(g => g.Id == gameId).CurrentTurn++;
            context.Games.SingleOrDefault(g => g.Id == gameId).LastTurnStart = DateTime.Now;
            context.SaveChanges();
        }
    }

    public static Asset GetSentinel(long perspective, long factionId, int turn)
    {
        Asset result = null;

        using (var context = new FactionRPG())
        {
            var assetIds = from o in context.Orders
                    where o.Perspective.Id == perspective
                    && o.Turn == turn
                    && o.IsSentinel
                    select o.AssetId;

            foreach (var assetId in assetIds)
            {
                if (Service.GetFactionOfAssetId(assetId).Id == factionId)
                {
                    result = Service.GetAsset(assetId);
                }
            }

            if (result == null)
            {
                result = context.Assets.FirstOrDefault<Asset>(
                    a => a.Faction.Id == factionId
                        && a.Turn == turn);

                var factionAssets = from a in context.Assets
                                    where a.Faction.Id == factionId && a.Turn == turn
                                    select a;

                foreach (var factionAsset in factionAssets)
                {
                    if (factionAsset.Value > result.Value)
                    {
                        result = factionAsset;
                    }
                }
            }
        }

        Service.SetSentinel(perspective, factionId, result, turn);

        return result;
    }

    public static void SetNotes(long factionId, string text)
    {
        using (var context = new FactionRPG())
        {
            context.Factions.SingleOrDefault(f => f.Id == factionId).Notes = text;
            context.SaveChanges();
        }
    }

    public static void SetGameLastTurnStart(long gameId, DateTime lastTurnStart)
    {
        using (var context = new FactionRPG())
        {
            context.Games.SingleOrDefault(g => g.Id == gameId).LastTurnStart = lastTurnStart;
            context.SaveChanges();
        }
    }

    //public static void SetTotalSpace(long gameId, int totalSpace)
    //{
    //    using (var context = new FactionRPG())
    //    {
    //        context.Games.SingleOrDefault(g => g.Id == gameId).TotalSpace = totalSpace;
    //        context.SaveChanges();
    //    }
    //}

    public static void SetSentinel(long perspective, long factionId, Asset Sentinel, int turn)
    {
        using (var context = new FactionRPG())
        {
            var orders = from o in context.Orders
                         where o.Perspective.Id == perspective
                         && o.Turn == turn
                         select o;



            foreach (var order in orders)
            {
                if (Service.GetFactionOfAssetId(order.AssetId).Id == factionId)
                {
                    order.IsSentinel = order.AssetId == Sentinel.Id;
                    //if (asset.IsSentinel)
                    //{
                    //    ClearCache("Asset_" + asset.Id);
                    //}
                }
            }

            context.SaveChanges();
        }
    }

    public static List<Asset> GetAssetsProfiledByFaction(long factionId, int turn)
    {
        using (var context = new FactionRPG())
        {
            return (from p in context.Profiles
                    where p.Asset.Turn == turn && p.Faction.Id == factionId
                    select p.Asset).ToList();
        }
    }

    public static Asset GetAssetOfProfile(long profileId)
    {
        using (var context = new FactionRPG())
        {
            return (from p in context.Profiles
                    where p.Id == profileId
                    select p.Asset).FirstOrDefault();
        }
    }

    public static List<long> GetProfileIdsOnAsset(long assetId)
    {
        using (var context = new FactionRPG())
        {
            return (from p in context.Profiles
                    where p.Asset.Id == assetId
                    select p.Id).ToList();
        }
    }

    public static void RemoveFaction(long factionId)
    {
        using (var context = new FactionRPG())
        {
            context.Factions.Remove(context.Factions.SingleOrDefault(f => f.Id == factionId));
            context.SaveChanges();
        }
    }

    public static void RemoveAsset(long assetId)
    {
        var profileIdsOnAsset = Service.GetProfileIdsOnAsset(assetId);

        using (var context = new FactionRPG())
        {
            foreach (var profileIdOnAsset in profileIdsOnAsset)
            {
                context.Profiles.Remove(context.Profiles.SingleOrDefault(p => p.Id == profileIdOnAsset));
            }

            context.Assets.Remove(context.Assets.SingleOrDefault(a => a.Id == assetId));
            context.SaveChanges();
        }

        //ClearCache("Asset_" + assetId);
    }

    public static Asset ResetResultsOnAsset(long assetId)
    {
        using (var context = new FactionRPG())
        {
            // reset all previous calculations on the asset
            var assetFromDB = context.Assets.SingleOrDefault(a => a.Id == assetId);

            assetFromDB.ValueLossFromDamage = 0;
            assetFromDB.ShroudedByFactionIds = string.Empty;
            assetFromDB.WasShrouded = false;
            assetFromDB.WasEnhanced = false;
            assetFromDB.WasInfusedWith = string.Empty;
            assetFromDB.WasSeizedByFactionId = 0;

            // clear all profiles gained on this asset
            var infiltrations = (from i in context.Infiltrations
                                 where i.Asset.Id == assetId
                                 select i).ToList();

            foreach (var infiltration in infiltrations)
            {
                context.Infiltrations.Remove(infiltration);
            }

            context.SaveChanges();

            //ClearCache("Asset_" + assetId);

            return assetFromDB;
        }
    }

    public static void AddInfiltration(long assetId, Infiltration infiltration)
    {
        using (var context = new FactionRPG())
        {
            var asset = context.Assets.SingleOrDefault(a => a.Id == assetId);
            asset.Infiltrations.Add(infiltration);
            context.SaveChanges();
        }

        //ClearCache("Asset_" + assetId);
    }

    public static void SetWasSeizedByFactionId(long assetId, long wasSeizedByFactionId)
    {
        using (var context = new FactionRPG())
        {
            var asset = context.Assets.SingleOrDefault(a => a.Id == assetId);
            asset.WasSeizedByFactionId = wasSeizedByFactionId;
            context.SaveChanges();
        }

        //ClearCache("Asset_" + assetId);
    }

    public static void SetWasShrouded(long assetId, bool wasShrouded)
    {
        using (var context = new FactionRPG())
        {
            var asset = context.Assets.SingleOrDefault(a => a.Id == assetId);
            asset.WasShrouded = wasShrouded;
            context.SaveChanges();
        }

        //ClearCache("Asset_" + assetId);
    }

    public static void SetWasEnhanced(long assetId, bool wasEnhanced)
    {
        using (var context = new FactionRPG())
        {
            var asset = context.Assets.SingleOrDefault(a => a.Id == assetId);
            asset.WasEnhanced = wasEnhanced;
            context.SaveChanges();
        }

        //ClearCache("Asset_" + assetId);
    }

    public static void SetWasInfusedWith(long assetId, string wasInfusedWith)
    {
        using (var context = new FactionRPG())
        {
            var asset = context.Assets.SingleOrDefault(a => a.Id == assetId);
            asset.WasInfusedWith = wasInfusedWith;
            context.SaveChanges();
        }

        //ClearCache("Asset_" + assetId);
    }

    public static void SetWasPropagatedWith(long assetId, string wasPropagatedWith)
    {
        using (var context = new FactionRPG())
        {
            var asset = context.Assets.SingleOrDefault(a => a.Id == assetId);
            asset.WasPropagatedWith = wasPropagatedWith;
            context.SaveChanges();
        }

        //ClearCache("Asset_" + assetId);
    }

    public static void AddShroudedByFaction(long assetId, long factionId)
    {
        using (var context = new FactionRPG())
        {
            var asset = context.Assets.SingleOrDefault(a => a.Id == assetId);
            asset.ShroudedByFactionIds += factionId + ",";
            context.SaveChanges();
        }

        //ClearCache("Asset_" + assetId);
    }

    public static void SetValueLossFromCompromised(long assetId, int valueLossFromCompromised)
    {
        using (var context = new FactionRPG())
        {
            var asset = context.Assets.SingleOrDefault(a => a.Id == assetId);
            asset.ValueLossFromCompromised = valueLossFromCompromised;
            context.SaveChanges();
        }

        //ClearCache("Asset_" + assetId);
    }

    public static void ChangeValueLossFromCompromisedByAmount(long assetId, int change)
    {
        using (var context = new FactionRPG())
        {
            var asset = context.Assets.SingleOrDefault(a => a.Id == assetId);
            asset.ValueLossFromCompromised += change;
            context.SaveChanges();
        }

        //ClearCache("Asset_" + assetId);
    }

    public static void SetValueLossFromDamage(long assetId, int valueLossFromDamage)
    {
        using (var context = new FactionRPG())
        {
            var asset = context.Assets.SingleOrDefault(a => a.Id == assetId);
            asset.ValueLossFromDamage = valueLossFromDamage;
            context.SaveChanges();
        }

        //ClearCache("Asset_" + assetId);
    }

    public static void IncrementValueIncreaseFromDamage(long assetId)
    {
        using (var context = new FactionRPG())
        {
            var asset = context.Assets.SingleOrDefault(a => a.Id == assetId);
            asset.ValueIncreaseFromDamage++;
            context.SaveChanges();
        }

        //ClearCache("Asset_" + assetId);
    }

    public static void ChangeValueLossFromDamageByAmount(long assetId, int change)
    {
        using (var context = new FactionRPG())
        {
            var asset = context.Assets.SingleOrDefault(a => a.Id == assetId);
            asset.ValueLossFromDamage += change;
            context.SaveChanges();
        }

        //ClearCache("Asset_" + assetId);
    }

    public static Faction GetFactionOfAsset(Asset asset)
    {
        try
        {
            Faction result;

            using (var context = new FactionRPG())
            {
                asset = context.Assets.SingleOrDefault(a => a.Id == asset.Id);
                result = context.Factions.SingleOrDefault(f => f.Id == asset.Faction.Id);
            }

            return result;
        }
        catch { return null; }
    }

    public static Faction GetFactionOfAssetId(long assetId)
    {
        try
        {
            Faction result;

            using (var context = new FactionRPG())
            {
                var asset = context.Assets.SingleOrDefault(a => a.Id == assetId);
                result = context.Factions.SingleOrDefault(f => f.Id == asset.Faction.Id);
            }

            return result;
        }
        catch { return null; }
    }

    public static Faction GetFactionOfProfile(Profile profile)
    {
        Faction result;

        using (var context = new FactionRPG())
        {
            profile = context.Profiles.SingleOrDefault(p => p.Id == profile.Id);
            result = context.Factions.SingleOrDefault(f => f.Id == profile.Faction.Id);
        }

        return result;
    }

    public static Asset GetAssetOfProfile(Profile profile)
    {
        Asset result;

        using (var context = new FactionRPG())
        {
            result = (from p in context.Profiles
                      where p.Id == profile.Id
                      select p.Asset).FirstOrDefault();
        }

        return result;
    }

    public static void AddFactionToGame(long gameId, Faction faction)
    {
        using (var context = new FactionRPG())
        {
            context.Games.SingleOrDefault(g => g.Id == gameId).Factions.Add(faction);
            context.SaveChanges();
        }
    }

    public static void AddAssetToFaction(long factionId, Asset asset)
    {
        using (var context = new FactionRPG())
        {
            context.Factions.SingleOrDefault(f => f.Id == factionId).Assets.Add(asset);
            context.SaveChanges();
        }
    }

    public static void AddProfileOnAssetToFaction(long factionId, Asset asset)
    {
        using (var context = new FactionRPG())
        {
            asset = context.Assets.SingleOrDefault(a => a.Id == asset.Id);

            var profile = new Profile()
            {
                Asset = asset,
            };

            context.Factions.SingleOrDefault(f => f.Id == factionId).Profiles.Add(profile);
            context.SaveChanges();
        }
    }

    public static void AddProfileOnAssetToFaction(long factionId, Asset asset, long senderFactionId)
    {
        using (var context = new FactionRPG())
        {
            asset = context.Assets.SingleOrDefault(a => a.Id == asset.Id);

            var profile = new Profile()
            {
                Asset = asset,
                SenderFactionId = senderFactionId,
            };

            context.Factions.SingleOrDefault(f => f.Id == factionId).Profiles.Add(profile);
            context.SaveChanges();
        }
    }

    public static List<Profile> GetProfilesOnAssetByFaction(int turn, Asset asset, long factionId)
    {
        using (var context = new FactionRPG())
        {
            return (from p in context.Profiles
                    where p.Asset.Turn == turn
                    && p.Faction.Id == factionId
                    && p.Asset.Name.Equals(asset.Name)
                    select p).ToList();
        }
    }

    public static List<Profile> GetProfilesSharedByFaction(long factionId, int turn)
    {
        using (var context = new FactionRPG())
        {
            return (from p in context.Profiles
                    where p.Asset.Turn == turn
                    && p.SenderFactionId == factionId
                    select p).ToList();
        }
    }

    public static void SetFactionName(long factionId, string name)
    {
        using (var context = new FactionRPG())
        {
            context.Factions.SingleOrDefault(f => f.Id == factionId).Name = name;
            context.SaveChanges();
        }
    }

    public static void SetCommitment(long factionId, bool committed)
    {
        using (var context = new FactionRPG())
        {
            context.Factions.SingleOrDefault(f => f.Id == factionId).Committed = committed;
            context.SaveChanges();
        }
    }

    public static void SetDefaultAction(long assetId)
    {
        using (var context = new FactionRPG())
        {
            var asset = context.Assets.SingleOrDefault(a => a.Id == assetId);

            Service.AddOrReplaceOrder(asset.Faction.Id, new Order()
            {
                Action = Order.Actions.Enhance.ToString(),
                AssetId = asset.Id,
                GoingPublic = false,
                InfuseWith = string.Empty,
                IsSentinel = false,
                ShotInTheDarkFactionId = 0,
                TargetId = asset.Id,
                Turn = asset.Turn,
                PropagateWith = string.Empty
            });
        }

        //ClearCache("Asset_" + assetId);
    }

    public static void SetTargetWhenSITD(long orderId)
    {
        using (var context = new FactionRPG())
        {
            var order = context.Orders.SingleOrDefault(o => o.Id == orderId);
            context.Orders.SingleOrDefault(o => o.Id == orderId).TargetId = 
                Service.GetSentinel(
                    Service.GetFactionOfAssetId(order.AssetId).Id,
                    order.ShotInTheDarkFactionId,
                    Service.GetAsset(order.AssetId).Turn
                ).Id;
            context.SaveChanges();
        }

        //ClearCache("Order_" + assetId);
    }

    public static void SetAssetOrders(
        string action,
        long assetId,
        long factionId,
        bool goingPublic,
        string infuseWith,
        bool isSentinel,
        long shotInTheDarkFactionId,
        long targetId,
        int turn,
        string propagateWith
        )
    {
        var assetOrder = new Order()
        {
            Action = action,
            AssetId = assetId,
            GoingPublic = goingPublic,
            InfuseWith = infuseWith,
            IsSentinel = isSentinel,
            ShotInTheDarkFactionId = shotInTheDarkFactionId,
            TargetId = targetId,
            Turn = turn,
            PropagateWith = propagateWith,
        };

        Service.AddOrReplaceOrder(factionId, assetOrder);

        //ClearCache("Order_" + assetId);
    }

    public static List<Asset> GetAssetsOfFaction(long factionId)
    {
        using (var context = new FactionRPG())
        {
            return (from a in context.Assets
                    where a.Faction.Id == factionId
                    select a).ToList();
        }
    }

    public static List<long> GetAssetIdsInGame(Game game)
    {
        using (var context = new FactionRPG())
        {
            return (from a in context.Assets
                    where a.Faction.Game.Id == game.Id
                    && a.Turn == game.CurrentTurn
                    select a.Id).ToList();
        }
    }

    public static List<long> GetAssetIdsTargetingAsset(long assetId, long perspective)
    {
        using (var context = new FactionRPG())
        {
            return (from o in context.Orders
                    where o.TargetId == assetId
                    && o.Perspective.Id == perspective
                    select o.AssetId).ToList();
        }
    }

    public static Asset GetAsset(long assetId)
    {
        //return Service.Get("Asset_" + assetId, () =>
        //{
        using (var context = new FactionRPG())
        {
            return context.Assets.SingleOrDefault(a => a.Id == assetId);
        }
        //});
    }

    public static Order GetOrder(long orderId)
    {
        //return Service.Get("Order_" + orderId, () =>
        //{
        using (var context = new FactionRPG())
        {
            return context.Orders.SingleOrDefault(o => o.Id == orderId);
        }
        //});
    }

    public static Order GetOrderOfAssetFromPerspective(long factionId, long assetId)
    {
        Order result;
        using (var context = new FactionRPG())
        {
            result = context.Orders.SingleOrDefault(o => o.Perspective.Id == factionId && o.AssetId == assetId);
        }
        if (result == null)
        {
            var asset = Service.GetAsset(assetId);

            // set the default action for the asset
            result = new Order()
            {
                AssetId = asset.Id,
                Turn = asset.Turn,
                Action = Order.Actions.Enhance.ToString(),
                TargetId = asset.Id,
            };

            Service.AddOrReplaceOrder(factionId, result);
        }
        return result;
    }

    public static Asset GetAsset(long gameId, string name)
    {
        using (var context = new FactionRPG())
        {
            return (from a in context.Assets
                    where a.Faction.Game.Id == gameId
                    && a.Name.Equals(name)
                    select a).FirstOrDefault();
        }
    }

    public static Faction GetFaction(long factionId)
    {
        using (var context = new FactionRPG())
        {
            return context.Factions.SingleOrDefault(f => f.Id == factionId);
        }
    }

    public static Faction GetFaction(long gameId, string membershipEmail)
    {
        using (var context = new FactionRPG())
        {
            return context.Factions.SingleOrDefault(f => f.Game.Id == gameId && f.PlayerEmail.Equals(membershipEmail));
        }
    }

    public static List<Faction> GetOtherFactions(Faction faction)
    {
        var game = Service.GetGameOfFaction(faction);

        using (var context = new FactionRPG())
        {
            return (from f in context.Factions
                    where f.Game.Id == game.Id
                    && f.Id != faction.Id
                    select f).ToList();
        }
    }

    public static void CreateGame(Game game)
    {
        using (var context = new FactionRPG())
        {
            context.Games.Add(game);
            context.SaveChanges();
        }
    }

    public static Game GetGame(long gameId)
    {
        using (var context = new FactionRPG())
        {
            return context.Games.SingleOrDefault(g => g.Id == gameId);
        }
    }

    public static Game GetGame(string guid)
    {
        using (var context = new FactionRPG())
        {
            return context.Games.SingleOrDefault(g => g.Guid.Equals(guid));
        }
    }

    public static Infiltration GetInfiltration(long infiltrationId)
    {
        using (var context = new FactionRPG())
        {
            return context.Infiltrations.SingleOrDefault(i => i.Id == infiltrationId);
        }
    }

    public static Profile GetProfile(long profileId)
    {
        using (var context = new FactionRPG())
        {
            return context.Profiles.SingleOrDefault(p => p.Id == profileId);
        }
    }

    public static Profile GetProfile(long factionId, long assetId, long senderFactionId)
    {
        using (var context = new FactionRPG())
        {
            return context.Profiles.SingleOrDefault(p =>
                                p.Faction.Id == factionId
                                && p.Asset.Id == assetId
                                && p.SenderFactionId == senderFactionId);
        }
    }

    public static List<long> GetProfileIdsOfFaction(long factionId)
    {
        using (var context = new FactionRPG())
        {
            return (from p in context.Profiles
                    where p.Faction.Id == factionId
                    select p.Asset.Id).ToList();
        }
    }

    public static List<long> GetProfileableAssetIdsOfFaction(long factionId, int turn)
    {
        using (var context = new FactionRPG())
        {
            return (from a in context.Assets
                    where a.Faction.Id == factionId
                    && a.Turn == turn
                    && a.Covert
                    && Service.GetOrderOfAssetFromPerspective(Service.GetFactionOfAsset(a).Id, a.Id).GoingPublic == false
                    select a.Id).ToList();
        }
    }

    public static List<Infiltration> GetInfiltrationsFromOneFactionOnAnother(long firstFaction, long secondFaction, int turn)
    {
        using (var context = new FactionRPG())
        {
            return (from i in context.Infiltrations
                    where i.FactionId == firstFaction
                    && i.Asset.Faction.Id == secondFaction
                    && i.Asset.Turn == turn
                    select i).ToList();
        }
    }

    public static List<long> GetInfiltrationIdsOnAssetFromPerspective(Game game, Asset asset, long perspective)
    {
        var collaboratingAssetIds = Service.GetCollaboratingAssetIds(asset, perspective);
        var visibleAssetsFromPerspective = Service.GetAssetsFromPerspective(game, perspective, asset.Turn);
        var visibleAssetIdsFromPerspective = visibleAssetsFromPerspective.Select(x => x.Id);
        var visibleFactionIdsFromPerspective = new List<long>();
        var target = Service.GetAsset(Service.GetOrderOfAssetFromPerspective(perspective, asset.Id).TargetId);

        using (var context = new FactionRPG())
        {
            foreach (var visibleAssetId in visibleAssetIdsFromPerspective)
            {
                if (collaboratingAssetIds.Contains(visibleAssetId))
                {
                    var visibleAsset = Service.GetAsset(visibleAssetId);
                    var factionId = Service.GetFactionOfAsset(visibleAsset).Id;

                    if (!visibleFactionIdsFromPerspective.Contains(factionId))
                    {
                        visibleFactionIdsFromPerspective.Add(factionId);
                    }
                }
            }

            return (from i in context.Infiltrations
                    where i.Asset.Id == target.Id
                    && visibleFactionIdsFromPerspective.Contains(i.FactionId)
                    select i.Id).ToList();
        }
    }

    public static List<Asset> GetAssets(List<long> assetIds)
    {
        using (var context = new FactionRPG())
        {
            return (from a in context.Assets
                    where assetIds.Contains(a.Id)
                    select a).ToList();
        }
    }

    public static List<Asset> GetAssetsWithSameNameInGame(Game game, Asset asset)
    {
        using (var context = new FactionRPG())
        {
            return (from a in context.Assets
                    where a.Faction.Game.Id == game.Id
                    && a.Name.Equals(asset.Name)
                    && (a.Value > 0 || (a.Value == 0 && a.Turn == game.CurrentTurn))
                    select a).ToList();
        }
    }

    public static List<Faction> GetFactions(List<long> factionIds)
    {
        using (var context = new FactionRPG())
        {
            return (from f in context.Factions
                    where factionIds.Contains(f.Id)
                    select f).ToList();
        }
    }

    public static List<Faction> GetFactionsInGame(long gameId)
    {
        using (var context = new FactionRPG())
        {
            return (from f in context.Factions
                    where f.Game.Id == gameId
                    select f).ToList();
        }
    }

    public static List<Faction> GetFactionsOfUser(string membershipEmail)
    {
        using (var context = new FactionRPG())
        {
            return (from f in context.Factions
                    where f.PlayerEmail.Equals(membershipEmail)
                    select f).ToList();
        }
    }

    public static Game GetGameOfAsset(Asset asset)
    {
        //Game result;

        using (var context = new FactionRPG())
        {
            return (from g in context.Games
                    where g.Factions.Any(f => f.Assets.Any(a => a.Id == asset.Id))
                    select g).FirstOrDefault();

            //long factionId = GetFactionOfAsset(asset).Id;
            //var faction = context.Factions.SingleOrDefault(f => f.Id == factionId);
            //result = context.Games.SingleOrDefault(g => g.Id == faction.Game.Id);
        }

        //return result;
    }

    public static Game GetGameOfFaction(Faction faction)
    {
        //Game result;

        using (var context = new FactionRPG())
        {
            return (from g in context.Games
                    where g.Factions.Any(f => f.Id == faction.Id)
                    select g).FirstOrDefault();

            //faction = context.Factions.SingleOrDefault(f => f.Id == faction.Id);
            //result = context.Games.SingleOrDefault(g => g.Id == faction.Game.Id);
        }

        //return result;
    }

    public static List<Game> GetGamesFacilitatedByUser(string membershipEmail)
    {
        using (var context = new FactionRPG())
        {
            return (from g in context.Games
                    where g.FacilitatorEmail.Equals(membershipEmail)
                    && !g.Factions.Any(f => f.PlayerEmail.Equals(membershipEmail))
                    select g).ToList();
        }
    }

    public static List<Game> GetGamesPlayedByUser(string membershipEmail)
    {
        using (var context = new FactionRPG())
        {
            return (from g in context.Games
                    where g.Factions.Any(f => f.PlayerEmail.Equals(membershipEmail))
                    && (g.TotalTurns == 0 || g.CurrentTurn <= g.TotalTurns)
                    select g).ToList();
        }
    }

    public static List<Game> GetGamesCompletedByUser(string membershipEmail)
    {
        using (var context = new FactionRPG())
        {
            return (from g in context.Games
                    where g.Factions.Any(f => f.PlayerEmail.Equals(membershipEmail))
                    && (g.TotalTurns > 0 && g.CurrentTurn > g.TotalTurns)
                    select g).ToList();
        }
    }

    public static List<long> GetCollaboratingAssetIds(Asset asset, long perspective)
    {
        var order = GetOrderOfAssetFromPerspective(perspective, asset.Id);

        using (var context = new FactionRPG())
        {
            return (from o in context.Orders
                    where o.Action.Equals(order.Action)
                    && o.TargetId == order.TargetId
                    && o.Perspective.Id == perspective
                    select o.AssetId).ToList();
        }
    }

    public static List<Asset> GetAssetsFromPerspective(Game game, long perspective, int turn)
    {
        using (var context = new FactionRPG())
        {
            var faction = (from f in context.Factions
                           where f.Game.Id == game.Id && f.Id == perspective
                           select f).FirstOrDefault();

            if (faction == null)
            {
                if (game.FacilitatorEmail.Equals(Service.GetFaction(perspective).PlayerEmail))
                {
                    return (from a in context.Assets
                            where a.Turn == turn
                            && a.Faction.Game.Id == game.Id
                            select a).ToList();
                }
                else
                {
                    return (from a in context.Assets
                            where a.Turn == turn
                            && a.Faction.Game.Id == game.Id
                            && !a.Covert
                            select a).ToList();
                }
            }
            else
            {
                var profiledAssets = (from p in context.Profiles
                                      where p.Asset.Turn == turn
                                      && p.Faction.Id == faction.Id
                                      select p.Asset).ToList();

                var publicAssets = (from a in context.Assets
                                    where a.Turn == turn
                                    && !a.Covert
                                    && a.Faction.Game.Id == faction.Game.Id
                                    select a).ToList();

                var allVisibleAssets = profiledAssets;
                allVisibleAssets.AddRange(publicAssets);

                return allVisibleAssets;
            }
        }
    }

    public static List<Asset> GetAssetsOfFactionFromPerspective(Game game, long perspective, int turn, long otherFactionId)
    {
        var result = new List<Asset>();

        var allVisibleAssets = Service.GetAssetsFromPerspective(game, perspective, turn);

        foreach (var asset in allVisibleAssets)
        {
            if (Service.GetFactionOfAsset(asset).Id == otherFactionId)
            {
                if (!result.Contains(asset))
                {
                    result.Add(asset);
                }
            }
        }

        return result;
    }

    public static void AddOrReplaceOrder(long factionId, Order order)
    {
        using (var context = new FactionRPG())
        {
            if (context.Orders.SingleOrDefault(o => o.AssetId == order.AssetId && o.Perspective.Id == factionId) != null)
            {
                context.Orders.SingleOrDefault(o => o.AssetId == order.AssetId && o.Perspective.Id == factionId).Action = order.Action;
                context.Orders.SingleOrDefault(o => o.AssetId == order.AssetId && o.Perspective.Id == factionId).AssetId = order.AssetId;
                context.Orders.SingleOrDefault(o => o.AssetId == order.AssetId && o.Perspective.Id == factionId).GoingPublic = order.GoingPublic;
                context.Orders.SingleOrDefault(o => o.AssetId == order.AssetId && o.Perspective.Id == factionId).InfuseWith = order.InfuseWith;
                context.Orders.SingleOrDefault(o => o.AssetId == order.AssetId && o.Perspective.Id == factionId).IsSentinel = order.IsSentinel;
                context.Orders.SingleOrDefault(o => o.AssetId == order.AssetId && o.Perspective.Id == factionId).ShotInTheDarkFactionId = order.ShotInTheDarkFactionId;
                context.Orders.SingleOrDefault(o => o.AssetId == order.AssetId && o.Perspective.Id == factionId).TargetId = order.TargetId;
                context.Orders.SingleOrDefault(o => o.AssetId == order.AssetId && o.Perspective.Id == factionId).Turn = order.Turn;
                context.Orders.SingleOrDefault(o => o.AssetId == order.AssetId && o.Perspective.Id == factionId).PropagateWith = order.PropagateWith;
            }
            else
            {
                context.Factions.SingleOrDefault(f => f.Id == factionId).Orders.Add(order);
            }

            context.SaveChanges();
        }
    }

    public static void SetFactionHexColour(long factionId, string hexColour)
    {
        using (var context = new FactionRPG())
        {
            context.Factions.SingleOrDefault(f => f.Id == factionId).HexColour = hexColour;
            context.SaveChanges();
        }
    }

    public static List<Game> GetAllScenarios()
    {
        var result = new List<Game>();

        using (var context = new FactionRPG())
        {
            result = (from g in context.Games
                      where g.ScenarioTurn >= 0
                      select g).ToList();
        }

        return result;
    }
}