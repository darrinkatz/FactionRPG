using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Threading;

public class Game
{
    public long Id { get; set; }
    public string Guid { get; set; }
    public string FacilitatorEmail { get; set; }
    public string Name { get; set; }
    public bool IsManaged { get; set; }
    public int TotalTurns { get; set; }
    public int ScenarioTurn { get; set; }

    public int CurrentTurn { get; set; }
    public DateTime LastTurnStart { get; set; }

    public int TurnLengthMinutes { get; set; }
    public int StartingBuildPoints { get; set; }
    public int MaxInitialAssetValue { get; set; }
    public bool CreateAssets { get; set; }
    //public int InitialFreeSpace { get; set; }
    //public int TotalSpace { get; set; }

    public ICollection<Faction> Factions { get; set; }

    public Game()
    {
        this.Guid = System.Guid.NewGuid().ToString();
        this.ScenarioTurn = -1;
        this.Factions = new List<Faction>();
    }
}

public static class GameExtensions
{
    public static string Url(this Game game)
    {
        return String.Format("/game.aspx?guid={0}", game.Guid);
    }

    public static string HistoryUrl(this Game game, int Turn)
    {
        return String.Format("/history.aspx?guid={0}&turn={1}", game.Guid, Turn);
    }

    public static TimeSpan TimeRemaining(this Game game)
    {
        return game.TurnEnd() - DateTime.Now;
    }

    public static DateTime TurnEnd(this Game game)
    {
        return game.LastTurnStart + new TimeSpan(0, game.TurnLengthMinutes, 0);
    }

    public static string TimeRemainingString(this Game game)
    {
        if (game.CurrentTurn > 0)
        {
            var timeRemaining = game.TimeRemaining();
            if (timeRemaining < new TimeSpan(0, 0, 0))
            {
                return "Game paused.";
            }
            else
            {
                return string.Format("Orders due in {0} d, {1} h, {2} m, {3} s.", timeRemaining.Days, timeRemaining.Hours, timeRemaining.Minutes, timeRemaining.Seconds);
            }
        }
        else
        {
            return "Has not yet begun.";
        }
    }

    //public static int FreeSpace(this Game game)
    //{
    //    int totalAssetValue = 0;

    //    foreach (var assetId in Service.GetAssetIdsInGame(game))
    //    {
    //        var asset = Service.GetAsset(assetId);

    //        if (asset.Turn == game.CurrentTurn)
    //        {
    //            totalAssetValue += asset.Value;
    //        }
    //    }

    //    return game.TotalSpace - totalAssetValue;
    //}

    public static void ProcessTurn(this Game game)
    {
        var gameMutex = new Mutex(true, game.Guid);

        try
        {
            // get latest game state
            int currentTurn = Service.GetGame(game.Id).CurrentTurn;

            gameMutex.WaitOne();

            // check that the turn still needs to be processed after picking up the thread again
            if (Service.GetGame(game.Id).CurrentTurn == currentTurn)
            {
                var allAssetIds = Service.GetAssetIdsInGame(game);
                var allTrueOrderIds = allAssetIds.Select(a => Service.GetOrderOfAssetFromPerspective(Service.GetFactionOfAssetId(a).Id, a).Id);
                var allFactions = Service.GetFactionsInGame(game.Id);

                // if asset was performing a Shot in the Dark, set target = faction's sentinel
                foreach (var trueOrderId in allTrueOrderIds)
                {
                    var order = Service.GetOrder(trueOrderId);

                    if (order.TargetId == -1 && order.ShotInTheDarkFactionId > 0)
                    {
                        try
                        {
                            Service.SetTargetWhenSITD(order.Id);
                        }
                        catch
                        {
                            // if target of SITD has no assets, swallow exception
                        }
                    }
                }

                // compute the result of every asset in the game
                foreach (var assetId in allAssetIds)
                {
                    var asset = Service.GetAsset(assetId);

                    asset.ComputeResult(Service.GetFactionOfAssetId(assetId).Id);
                }

                // create the game state for the next turn
                foreach (var assetId in allAssetIds)
                {
                    var asset = Service.GetAsset(assetId);
                    var factionIdOfAsset = Service.GetFactionOfAsset(asset).Id;

                    // if asset was not destroyed, duplicate it to next turn
                    if ((asset.Value > 0 && asset.Value > asset.ValueLossFromCompromised + asset.ValueLossFromDamage) || (asset.Value == 0 && asset.WasEnhanced))
                    {
                        var newAsset = new Asset()
                        {
                            Turn = game.CurrentTurn + 1,
                            Value = asset.Value
                                + asset.ValueIncreaseFromDamage
                                - asset.ValueLossFromCompromised
                                - asset.ValueLossFromDamage
                                + (asset.WasEnhanced ? 1 : 0),
                            Name = asset.Name,
                            Covert = asset.WasShrouded || (asset.Covert && !Service.GetOrderOfAssetFromPerspective(factionIdOfAsset, asset.Id).GoingPublic),
                            ImageUrl = asset.ImageUrl,

                            HasAmbush = asset.HasAmbush || asset.WasPropagatedWith.Contains(Asset.Specialziations.Ambush.ToString()),
                            HasDisguise = asset.HasDisguise || asset.WasPropagatedWith.Contains(Asset.Specialziations.Disguise.ToString()),
                            HasInfuse = asset.HasInfuse || asset.WasPropagatedWith.Contains(Asset.Specialziations.Infuse.ToString()),
                            HasInvestigate = asset.HasInvestigate || asset.WasPropagatedWith.Contains(Asset.Specialziations.Investigate.ToString()),
                            HasManeuver = asset.HasManeuver || asset.WasPropagatedWith.Contains(Asset.Specialziations.Maneuver.ToString()),
                            HasSalvage = asset.HasSalvage || asset.WasPropagatedWith.Contains(Asset.Specialziations.Salvage.ToString()),
                            HasPropagate = asset.HasPropagate || asset.WasPropagatedWith.Contains(Asset.Specialziations.Propagate.ToString()),
                            HasVanish = asset.HasVanish || asset.WasPropagatedWith.Contains(Asset.Specialziations.Vanish.ToString()),

                            InfusedWithAmbush = asset.WasInfusedWith.Contains(Asset.Specialziations.Ambush.ToString()),
                            InfusedWithDisguise = asset.WasInfusedWith.Contains(Asset.Specialziations.Disguise.ToString()),
                            InfusedWithInfuse = asset.WasInfusedWith.Contains(Asset.Specialziations.Infuse.ToString()),
                            InfusedWithInvestigate = asset.WasInfusedWith.Contains(Asset.Specialziations.Investigate.ToString()),
                            InfusedWithManeuver = asset.WasInfusedWith.Contains(Asset.Specialziations.Maneuver.ToString()),
                            InfusedWithSalvage = asset.WasInfusedWith.Contains(Asset.Specialziations.Salvage.ToString()),
                            InfusedWithPropagate = asset.WasInfusedWith.Contains(Asset.Specialziations.Propagate.ToString()),
                            InfusedWithVanish = asset.WasInfusedWith.Contains(Asset.Specialziations.Vanish.ToString()),
                        };
                        
                        long owningFactionId;

                        // if asset was seized, give asset to that faction
                        if (asset.WasSeizedByFactionId > 0)
                        {
                            owningFactionId = asset.WasSeizedByFactionId;
                        }
                        else
                        {
                            // otherwise it stays with previous owner
                            owningFactionId = factionIdOfAsset;
                        }

                        Service.AddAssetToFaction(owningFactionId, newAsset);

                        foreach (var faction in allFactions)
                        {
                            // set the default action for the asset
                            var newOrder = new Order()
                            {
                                AssetId = newAsset.Id,
                                Turn = newAsset.Turn,
                                Action = Order.Actions.Enhance.ToString(),
                                TargetId = newAsset.Id,
                            };

                            Service.AddOrReplaceOrder(faction.Id, newOrder);
                        }

                        // if not shrouded and still hidden, duplicate profiles for asset
                        if (!asset.WasShrouded && asset.Covert && !Service.GetOrderOfAssetFromPerspective(factionIdOfAsset, asset.Id).GoingPublic)
                        {
                            var profileIdsOnThisAsset = Service.GetProfileIdsOnAsset(asset.Id);

                            foreach (var profileId in profileIdsOnThisAsset)
                            {
                                var profile = Service.GetProfile(profileId);
                                var factionIdOfProfile = Service.GetFactionOfProfile(profile).Id;

                                // if this profile has not already been duplicated, copy it to next turn
                                var existingProfiles = Service.GetProfilesOnAssetByFaction(game.CurrentTurn + 1, Service.GetAssetOfProfile(profile), factionIdOfProfile);

                                if (existingProfiles.Count() == 0)
                                {
                                    Service.AddProfileOnAssetToFaction(factionIdOfProfile, newAsset);
                                }
                            }
                        }
                        else
                        {
                            if (asset.WasShrouded)
                            {
                                // every asset that collaborated on shrouding it gains a profile on it
                                foreach (var factionIdString in asset.ShroudedByFactionIds.Split(','))
                                {
                                    long factionId;
                                    if (long.TryParse(factionIdString, out factionId))
                                    {
                                        // if this faction does not already have a profile on this asset
                                        var existingProfiles = Service.GetProfilesOnAssetByFaction(game.CurrentTurn + 1, asset, factionId);

                                        if (existingProfiles.Count() == 0)
                                        {
                                            Service.AddProfileOnAssetToFaction(factionId, newAsset);
                                        }
                                    }
                                }

                                // if asset was not seized, the owner also gets a profile on it even if it didn't participate in the shroud
                                if (asset.WasSeizedByFactionId == 0)
                                {
                                    var existingProfiles = Service.GetProfilesOnAssetByFaction(game.CurrentTurn + 1, asset, factionIdOfAsset);

                                    if (existingProfiles.Count() == 0)
                                    {

                                        Service.AddProfileOnAssetToFaction(factionIdOfAsset, newAsset);
                                    }
                                }
                            }
                        }
                    }
                }

                // once all assets have been processed, deal with profiles gained via infiltrate
                var allOtherFactions = Service.GetFactionsInGame(game.Id);

                // for each faction,
                foreach (var faction in allFactions)
                {
                    // for each other faction
                    foreach (var otherFaction in allOtherFactions)
                    {
                        // count how many profiles have been gained on the other faction this turn
                        var infiltrations = Service.GetInfiltrationsFromOneFactionOnAnother(faction.Id, otherFaction.Id, game.CurrentTurn);

                        var numProfiles = 0;
                        foreach (var infiltration in infiltrations)
                        {
                            numProfiles += infiltration.ProfileCount;
                        }

                        // randomly
                        // gain profiles on the target's hidden assets from next turn
                        // on which faction does not already have a profile
                        if (numProfiles > 0)
                        {
                            var newlyProfiledAssetIds = Service.GetProfileIdsOfFaction(faction.Id);

                            // all potential hidden assets to profile
                            var allProfileableAssetIds = Service.GetProfileableAssetIdsOfFaction(otherFaction.Id, game.CurrentTurn + 1).Where(a => !newlyProfiledAssetIds.Contains(a)).ToList();

                            // hidden assets which have actually been profiled
                            var hiddenAssetIds = new List<long>();

                            // choose profiles at random
                            for (int i = 0; i < numProfiles; i++)
                            {
                                if (allProfileableAssetIds.Count > 0)
                                {
                                    var assetId = allProfileableAssetIds.Skip(new Random().Next(allProfileableAssetIds.Count)).First();
                                    hiddenAssetIds.Add(assetId);
                                    allProfileableAssetIds.Remove(assetId);
                                }
                            }

                            // actually gain profiles
                            foreach (var hiddenAssetId in hiddenAssetIds)
                            {
                                var hiddenAsset = Service.GetAsset(hiddenAssetId);

                                Service.AddProfileOnAssetToFaction(faction.Id, hiddenAsset);
                            }
                        }
                    }
                }

                // un-commit every faction
                foreach (var faction in allFactions)
                {
                    Service.SetCommitment(faction.Id, false);
                }

                // make sure every faction has their sentinel assigned
                foreach (var faction in allFactions)
                {
                    foreach (var otherFaction in allOtherFactions)
                    {
                        Service.GetSentinel(faction.Id, otherFaction.Id, game.CurrentTurn + 1);
                    }
                }

                Service.IncrementGameTurn(game.Id);
            }
        }
        catch
        {
            // roll back transaction?
            gameMutex.ReleaseMutex();
            throw;
        }
        finally
        {
            gameMutex.ReleaseMutex();
        }
    }

    public static Game CreateScenario(this Game game, int turn, string perspective)
    {
        var newGame = new Game
        {
            FacilitatorEmail = perspective,
            Name = game.Name,
            TotalTurns = game.TotalTurns,
            IsManaged = game.IsManaged,
            CurrentTurn = 0,
            LastTurnStart = DateTime.Now,
            TurnLengthMinutes = game.TurnLengthMinutes,
            StartingBuildPoints = game.StartingBuildPoints,
            MaxInitialAssetValue = game.MaxInitialAssetValue,
            CreateAssets = game.CreateAssets,
            //InitialFreeSpace = int.Parse(txtFreeSpace.Text),
        };

        Service.CreateGame(newGame);

        // TODO: add all factions & assets in current game to new game as Turn 0

        return newGame;
    }
}