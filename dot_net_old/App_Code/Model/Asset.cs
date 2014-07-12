using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Transactions;
using System.Collections;

public class Asset
{
    // Properties
    public long Id { get; set; }
    public virtual Faction Faction { get; set; }
    public int Turn { get; set; }
    public int Value { get; set; }
    public string Name { get; set; }
    public bool Covert { get; set; }
    public string ImageUrl { get; set; }
    //public long TheorizedByFactionId { get; set; }

    public ICollection<Infiltration> Infiltrations { get; set; }

    // Specializations
    public bool HasAmbush { get; set; }
    public bool HasDisguise { get; set; }
    public bool HasInfuse { get; set; }
    public bool HasInvestigate { get; set; }
    public bool HasManeuver { get; set; }
    public bool HasSalvage { get; set; }
    public bool HasPropagate { get; set; }
    public bool HasVanish { get; set; }

    // Infusions
    public bool InfusedWithAmbush { get; set; }
    public bool InfusedWithSalvage { get; set; }
    public bool InfusedWithDisguise { get; set; }
    public bool InfusedWithVanish { get; set; }
    public bool InfusedWithManeuver { get; set; }
    public bool InfusedWithInfuse { get; set; }
    public bool InfusedWithInvestigate { get; set; }
    public bool InfusedWithPropagate { get; set; }

    // Result Data
    public int ValueLossFromCompromised { get; set; }
    public int ValueLossFromDamage { get; set; }
    public int ValueIncreaseFromDamage { get; set; }
    public string ExpectedValueChange { get; set; }
    public string ShroudedByFactionIds { get; set; }
    public bool WasShrouded { get; set; }
    public bool WasEnhanced { get; set; }
    public string WasInfusedWith { get; set; }
    public string WasPropagatedWith { get; set; }
    public long WasSeizedByFactionId { get; set; }

    public Asset()
    {
        Infiltrations = new List<Infiltration>();
        WasInfusedWith = string.Empty;
        WasPropagatedWith = string.Empty;
    }

    public enum Specialziations
    {
        Ambush,
        Disguise,
        Infuse,
        Investigate,
        Maneuver,
        Salvage,
        Propagate,
        Vanish
    }
}

public static class AssetExtensions
{
    public static int BP(this Asset asset)
    {
        int result = asset.Value;

        if (asset.Covert) { result++; }

        if (asset.HasAmbush) { result++; }
        if (asset.HasSalvage) { result++; }
        if (asset.HasDisguise) { result++; }
        if (asset.HasVanish) { result++; }
        if (asset.HasManeuver) { result++; }
        if (asset.HasInfuse) { result++; }
        if (asset.HasInvestigate) { result++; }
        if (asset.HasPropagate) { result++; }

        return result;
    }

    public static string GetOrder(this Asset asset, long perspective, bool historical)
    {
        var result = string.Empty;
        var game = Service.GetGameOfAsset(asset);
        var visibleAssetIds = Service.GetAssetsFromPerspective(game, perspective, asset.Turn).Select(a => a.Id).ToList();
        var factionOfAsset = Service.GetFactionOfAsset(asset);
        //var assetIdsFromSameFaction = Service.GetAssetsOfFaction(factionOfAsset.Id).Select(a => a.Id).ToList();
        var totalStrength = 0;

        var collaborators = string.Empty;
        foreach (var collaboratorId in Service.GetCollaboratingAssetIds(asset, perspective))
        {
            var collaborator = Service.GetAsset(collaboratorId);

            if (collaborator.Value > 0)
            {
                if (visibleAssetIds.Contains(collaborator.Id))
                {
                    //if ((!historical && assetIdsFromSameFaction.Contains(collaborator.Id)) || historical)
                    //{
                        collaborators += collaborator.Render(perspective) + "; ";

                        if (!historical)
                        {
                            totalStrength += collaborator.Strength(perspective);
                        }
                    //}
                }
            }
        }

        var infusionString = asset.InfuseWithResult(perspective);
        if (!string.IsNullOrEmpty(infusionString))
        {
            infusionString = " [infuse with " + infusionString + "]";
        }

        var propagateString = asset.PropagateWithResult(perspective);
        if (!string.IsNullOrEmpty(propagateString))
        {
            propagateString = " {propagate with " + propagateString + "}";
        }

        var trueOrder = Service.GetOrderOfAssetFromPerspective(perspective, asset.Id);

        if (!historical && trueOrder.ShotInTheDarkFactionId > 0)
        {
            result = String.Format("{0} <strong>taking a {1}'shot in the dark' against</strong> {2}",
                collaborators,
                historical ? string.Empty : totalStrength + "-",
                Service.GetFaction(trueOrder.ShotInTheDarkFactionId).Name
                );
        }
        else
        {
            result = String.Format("{0} <strong>{1}{2}</strong>{3}{4} {5}",
                string.IsNullOrEmpty(collaborators) ? "???" : collaborators,
                historical ? string.Empty : totalStrength + "-",
                trueOrder.Action,
                infusionString,
                propagateString,
                visibleAssetIds.Contains(trueOrder.TargetId) ? Service.GetAsset(trueOrder.TargetId).Render(perspective) : "???"
                );
        }

        return result;
    }

    public static string Render(this Asset asset, long perspective)
    {
        var specializations = string.Empty;

        if (asset.HasAmbush) { specializations += Asset.Specialziations.Ambush.ToString() + ";"; }
        if (asset.HasSalvage) { specializations += Asset.Specialziations.Salvage.ToString() + ";"; }
        if (asset.HasDisguise) { specializations += Asset.Specialziations.Disguise.ToString() + ";"; }
        if (asset.HasVanish) { specializations += Asset.Specialziations.Vanish.ToString() + ";"; }
        if (asset.HasManeuver) { specializations += Asset.Specialziations.Maneuver.ToString() + ";"; }
        if (asset.HasInfuse) { specializations += Asset.Specialziations.Infuse.ToString() + ";"; }
        if (asset.HasInvestigate) { specializations += Asset.Specialziations.Investigate.ToString() + ";"; }
        if (asset.HasPropagate) { specializations += Asset.Specialziations.Propagate.ToString() + ";"; }

        if (!string.IsNullOrEmpty(specializations))
        {
            specializations = " {" + specializations + "}";
        }

        var infusions = string.Empty;

        if (asset.InfusedWithAmbush) { infusions += Asset.Specialziations.Ambush.ToString() + ";"; }
        if (asset.InfusedWithSalvage) { infusions += Asset.Specialziations.Salvage.ToString() + ";"; }
        if (asset.InfusedWithDisguise) { infusions += Asset.Specialziations.Disguise.ToString() + ";"; }
        if (asset.InfusedWithVanish) { infusions += Asset.Specialziations.Vanish.ToString() + ";"; }
        if (asset.InfusedWithManeuver) { infusions += Asset.Specialziations.Maneuver.ToString() + ";"; }
        if (asset.InfusedWithInfuse) { infusions += Asset.Specialziations.Infuse.ToString() + ";"; }
        if (asset.InfusedWithInvestigate) { infusions += Asset.Specialziations.Investigate.ToString() + ";"; }
        if (asset.InfusedWithPropagate) { infusions += Asset.Specialziations.Propagate.ToString() + ";"; }

        if (!string.IsNullOrEmpty(infusions))
        {
            infusions = " [infused with " + infusions + "]";
        }

        var order = Service.GetOrderOfAssetFromPerspective(perspective, asset.Id);
        var goingPublic = order != null && Service.GetOrderOfAssetFromPerspective(perspective, asset.Id).GoingPublic;
        var ownAsset = Service.GetFactionOfAsset(asset).Id == perspective;

        return string.Format("{0}{1}-{2}{3}{4}",
            asset.Value,
            asset.Covert ? goingPublic && ownAsset ? "!" : "?" : "",
            asset.Name,
            specializations,
            infusions
            );
    }

    public static List<Asset> OneAssetPerActionTargetingThis(this Asset asset, long perspective)
    {
        var result = new List<long>();
        List<long> allAssetIdsTargetingThis = Service.GetAssetIdsTargetingAsset(asset.Id, perspective);

        foreach (var assetIdTargetingThis in allAssetIdsTargetingThis)
        {
            if (!result.Contains(assetIdTargetingThis))
            {
                bool thisIsTargetedByAnyCollaboratingAssets = false;
                var collaboratingAssetIds = Service.GetCollaboratingAssetIds(Service.GetAsset(assetIdTargetingThis), perspective);
                foreach (var collaboratingAssetId in collaboratingAssetIds)
                {
                    if (result.Contains(collaboratingAssetId))
                    {
                        thisIsTargetedByAnyCollaboratingAssets = true;
                    }
                }
                if (!thisIsTargetedByAnyCollaboratingAssets)
                {
                    result.Add(assetIdTargetingThis);
                }
            }
        }

        return Service.GetAssets(result);
    }

    public static int Defense(this Asset asset, long perspective)
    {
        var result = asset.Value;

        var actions = asset.OneAssetPerActionTargetingThis(perspective);
        foreach (var actionTargetingThis in actions)
        {
            var trueOrder = Service.GetOrderOfAssetFromPerspective(perspective, actionTargetingThis.Id);

            if (trueOrder.Action.Equals(Order.Actions.Enhance.ToString())
                || trueOrder.Action.Equals(Order.Actions.Shroud.ToString()))
            {
                if (actionTargetingThis.SpecializationAccess(perspective).Contains(Asset.Specialziations.Ambush.ToString()) && trueOrder.Action.Equals(Order.Actions.Shroud.ToString()))
                {
                    result += 2 * actionTargetingThis.ActionStrength(perspective);
                }
                else
                {
                    result += actionTargetingThis.ActionStrength(perspective);
                }
            }
        }

        return result;
    }

    public static int Strength(this Asset asset, long perspective)
    {
        bool actionHasDisguise = asset.SpecializationAccess(perspective).Contains(Asset.Specialziations.Disguise.ToString());

        if (!asset.Covert || Service.GetOrderOfAssetFromPerspective(perspective, asset.Id).GoingPublic || actionHasDisguise)
        {
            return asset.Value;
        }
        else
        {
            // covert 0-value assets contribute a minimum strength of 0
            return Math.Max(asset.Value - 1, 0);
        }
    }

    public static int ActionStrength(this Asset asset, long perspective)
    {
        var result = 0;

        foreach (var collaboratingAssetId in Service.GetCollaboratingAssetIds(asset, perspective))
        {
            result += Service.GetAsset(collaboratingAssetId).Strength(perspective);
        }

        return result;
    }

    public static string InfuseWithResult(this Asset asset, long perspective)
    {
        var result = string.Empty;

        var collaboratorIds = Service.GetCollaboratingAssetIds(asset, perspective);
        var collaborators = Service.GetAssets(collaboratorIds);
        collaborators.Sort(new AssetStrengthDescendingComparer(perspective));

        foreach (var collaborator in collaborators)
        {
            var trueOrder = Service.GetOrderOfAssetFromPerspective(perspective, collaborator.Id);

            if (string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(trueOrder.InfuseWith))
            {
                result = trueOrder.InfuseWith;
                break;
            }
        }

        return result;
    }

    public static string PropagateWithResult(this Asset asset, long perspective)
    {
        var result = string.Empty;

        var collaboratorIds = Service.GetCollaboratingAssetIds(asset, perspective);
        var collaborators = Service.GetAssets(collaboratorIds);
        collaborators.Sort(new AssetStrengthDescendingComparer(perspective));

        foreach (var collaborator in collaborators)
        {
            var trueOrder = Service.GetOrderOfAssetFromPerspective(perspective, collaborator.Id);

            if (string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(trueOrder.PropagateWith))
            {
                result = trueOrder.PropagateWith;
                break;
            }
        }

        return result;
    }

    public static List<string> SpecializationAccess(this Asset asset, long perspective)
    {
        var result = new List<string>();

        foreach (var collaboratorId in Service.GetCollaboratingAssetIds(asset, perspective))
        {
            var collaborator = Service.GetAsset(collaboratorId);

            if ((collaborator.HasAmbush || collaborator.InfusedWithAmbush) && !result.Contains(Asset.Specialziations.Ambush.ToString())) { result.Add(Asset.Specialziations.Ambush.ToString()); }
            if ((collaborator.HasDisguise || collaborator.InfusedWithDisguise) && !result.Contains(Asset.Specialziations.Disguise.ToString())) { result.Add(Asset.Specialziations.Disguise.ToString()); }
            if ((collaborator.HasInfuse || collaborator.InfusedWithInfuse) && !result.Contains(Asset.Specialziations.Infuse.ToString())) { result.Add(Asset.Specialziations.Infuse.ToString()); }
            if ((collaborator.HasInvestigate || collaborator.InfusedWithInvestigate) && !result.Contains(Asset.Specialziations.Investigate.ToString())) { result.Add(Asset.Specialziations.Investigate.ToString()); }
            if ((collaborator.HasManeuver || collaborator.InfusedWithManeuver) && !result.Contains(Asset.Specialziations.Maneuver.ToString())) { result.Add(Asset.Specialziations.Maneuver.ToString()); }
            if ((collaborator.HasSalvage || collaborator.InfusedWithSalvage) && !result.Contains(Asset.Specialziations.Salvage.ToString())) { result.Add(Asset.Specialziations.Salvage.ToString()); }
            if ((collaborator.HasPropagate || collaborator.InfusedWithPropagate) && !result.Contains(Asset.Specialziations.Propagate.ToString())) { result.Add(Asset.Specialziations.Propagate.ToString()); }
            if ((collaborator.HasVanish || collaborator.InfusedWithVanish) && !result.Contains(Asset.Specialziations.Vanish.ToString())) { result.Add(Asset.Specialziations.Vanish.ToString()); }
        }

        return result;
    }

    public static void ChangeValueOfCollaborators(this Asset asset, int change, long perspective)
    {
        if (change != 0)
        {
            var collaboratorIds = Service.GetCollaboratingAssetIds(asset, perspective);

            var weightedList = new List<Asset>();
            int totalValue = 0;
            foreach (var collaboratorId in collaboratorIds)
            {
                var collaborator = Service.GetAsset(collaboratorId);

                for (int i = 0; i < collaborator.Value; i++)
                {
                    weightedList.Add(collaborator);
                }

                totalValue += collaborator.Value;
            }

            if (change <= totalValue * -1)
            {
                foreach (var collaboratorId in collaboratorIds)
                {
                    var collaborator = Service.GetAsset(collaboratorId);

                    Service.SetValueLossFromCompromised(collaborator.Id, collaborator.Value);
                }
            }
            else
            {
                Random rand = new Random();

                for (int i = 0; i < Math.Abs(change); i++)
                {
                    // randomly select an index from list
                    var collaborator = weightedList[rand.Next(weightedList.Count())];

                    // increase or decrease value
                    if (change > 0)
                    {
                        Service.IncrementValueIncreaseFromDamage(collaborator.Id);
                    }
                    else
                    {
                        Service.ChangeValueLossFromCompromisedByAmount(collaborator.Id, 1);

                        // if this asset has been destroyed, don't pick it again
                        if (Service.GetAsset(collaborator.Id).ValueLossFromCompromised >= Service.GetAsset(collaborator.Id).Value)
                        {
                            weightedList.RemoveAll(a => a.Id == collaborator.Id);
                        }
                    }
                }
            }
        }
    }

    public static void ComputeResult(this Asset asset, long perspective)
    {
        //asset = Service.ResetResultsOnAsset(asset.Id);

        int defense = asset.Defense(perspective);
        var actionSpecializationAccess = new Dictionary<long, List<string>>();
        var actionInfuseWithTotal = new Dictionary<long, string>();
        var actionPropagateWithTotal = new Dictionary<long, string>();

        var assetsTargetingThis = asset.OneAssetPerActionTargetingThis(perspective);
        assetsTargetingThis.Sort(new AssetStrengthDescendingComparer(perspective));

        var damageActionsTargetingThis = new List<Asset>();
        var infiltrateActionsTargetingThis = new List<Asset>();
        var enhanceActionsTargetingThis = new List<Asset>();
        var shroudActionsTargetingThis = new List<Asset>();

        foreach (var assetTargetingThis in assetsTargetingThis)
        {
            var action = Service.GetOrderOfAssetFromPerspective(perspective, assetTargetingThis.Id);

            // reset all previous calculations on assets targeting this
            //foreach (var collaborator in Service.GetCollaboratingAssets(action))
            //{
            //    Service.SetValueLossFromCompromised(collaborator.Id, 0);
            //    Service.SetValueLossFromDamage(collaborator.Id, 0);
            //}

            actionSpecializationAccess.Add(action.Id, assetTargetingThis.SpecializationAccess(perspective));
            actionInfuseWithTotal.Add(action.Id, assetTargetingThis.InfuseWithResult(perspective));
            actionPropagateWithTotal.Add(action.Id, assetTargetingThis.PropagateWithResult(perspective));

            switch (action.Action)
            {
                case "Damage":
                    damageActionsTargetingThis.Add(assetTargetingThis);
                    break;
                case "Infiltrate":
                    infiltrateActionsTargetingThis.Add(assetTargetingThis);
                    break;
                case "Enhance":
                    enhanceActionsTargetingThis.Add(assetTargetingThis);
                    break;
                case "Shroud":
                    shroudActionsTargetingThis.Add(assetTargetingThis);
                    break;
            }
        }

        bool wasDamaged = false;

        foreach (var action in damageActionsTargetingThis)
        {
            int strength = action.ActionStrength(perspective);

            // if the action was successful
            if (strength > defense)
            {
                wasDamaged = true;

                // if this attack was an exceptional success
                if (strength - defense >= asset.Value)
                {
                    // mark it as destroyed
                    Service.SetValueLossFromDamage(asset.Id, asset.Value);

                    // attackers gain value for destroying an asset
                    if (asset.Value > 1)
                    {
                        action.ChangeValueOfCollaborators(asset.Value - 1, perspective);
                    }
                }
                else
                {
                    // reduce its value
                    Service.ChangeValueLossFromDamageByAmount(asset.Id, strength - defense);

                    // attackers gain value if they have Salvage
                    if (actionSpecializationAccess[action.Id].Contains(Asset.Specialziations.Salvage.ToString()))
                    {
                        if (asset.Value > 1)
                        {
                            action.ChangeValueOfCollaborators(strength - defense - 1, perspective);
                        }
                    }
                }
            }
            else
            {
                // attackers are compromized unless they have Maneuver
                if (!actionSpecializationAccess[action.Id].Contains(Asset.Specialziations.Maneuver.ToString()))
                {
                    action.ChangeValueOfCollaborators(strength - defense, perspective);
                }
            }
        }

        foreach (var action in shroudActionsTargetingThis)
        {
            int strength = action.ActionStrength(perspective);

            // if defensive actions have not been thwarted on this asset
            if (!wasDamaged)
            {
                // if the action was exceptionally successful
                if (strength > asset.Value)
                {
                    Service.SetWasShrouded(asset.Id, true);

                    // collaborating factions will have a profile on this asset next turn
                    var factionIds = new List<long>();
                    foreach (var collaboratorId in Service.GetCollaboratingAssetIds(action, perspective))
                    {
                        var collaboratingAsset = Service.GetAsset(collaboratorId);
                        var factionIdOfAsset = Service.GetFactionOfAsset(collaboratingAsset);

                        if (!factionIds.Contains(factionIdOfAsset.Id))
                        {
                            factionIds.Add(factionIdOfAsset.Id);
                        }
                    }

                    foreach (var factionId in factionIds)
                    {
                        Service.AddShroudedByFaction(asset.Id, factionId);
                    }
                }
            }
        }

        foreach (var action in enhanceActionsTargetingThis)
        {
            int strength = action.ActionStrength(perspective);

            // if defensive actions have not been thwarted on this asset
            if (!wasDamaged)
            {
                // if the action was exceptionally successful
                if (strength > asset.Value)
                {
                    Service.SetWasEnhanced(asset.Id, true);
                    Service.SetWasInfusedWith(asset.Id, actionInfuseWithTotal[action.Id]);
                    if (actionSpecializationAccess[action.Id].Contains(actionPropagateWithTotal[action.Id]))
                    {
                        Service.SetWasPropagatedWith(asset.Id, actionPropagateWithTotal[action.Id]);
                    }
                }
            }
        }

        foreach (var action in infiltrateActionsTargetingThis)
        {
            int strength = action.ActionStrength(perspective);

            if (strength > defense)
            {
                // make list of collaborating factions and their contributions
                var infiltratingFactionIds = new List<long>();
                var factionStrengthContribution = new Dictionary<long, int>();

                foreach (var collaboratorId in Service.GetCollaboratingAssetIds(action, perspective))
                {
                    var collaboratingAsset = Service.GetAsset(collaboratorId);
                    var factionOfCollaboratingAsset = Service.GetFactionOfAsset(collaboratingAsset);

                    if (!infiltratingFactionIds.Contains(factionOfCollaboratingAsset.Id))
                    {
                        infiltratingFactionIds.Add(factionOfCollaboratingAsset.Id);
                    }

                    if (Service.GetOrderOfAssetFromPerspective(Service.GetFactionOfAssetId(collaboratorId).Id, collaboratorId).ShotInTheDarkFactionId == 0)
                    {
                        var hasDisguise = actionSpecializationAccess[action.Id].Contains(Asset.Specialziations.Disguise.ToString());
                        var contribution = !collaboratingAsset.Covert || hasDisguise ? collaboratingAsset.Value : collaboratingAsset.Value - 1;

                        if (factionStrengthContribution.ContainsKey(factionOfCollaboratingAsset.Id))
                        {
                            factionStrengthContribution[factionOfCollaboratingAsset.Id] += contribution;
                        }
                        else
                        {
                            factionStrengthContribution.Add(factionOfCollaboratingAsset.Id, contribution);
                        }
                    }
                }

                // if this attack was an exceptional success
                if (strength - defense >= asset.Value)
                {
                    if (factionStrengthContribution.Count > 0)
                    {
                        // the faction that contributed the most value to the attack seizes control
                        foreach (var factionContribution in factionStrengthContribution)
                        {
                            if (factionContribution.Value > 0)
                            {
                                if (asset.WasSeizedByFactionId == 0 || factionContribution.Value > factionStrengthContribution[asset.WasSeizedByFactionId])
                                {
                                    asset.WasSeizedByFactionId = factionContribution.Key;
                                }
                            }
                        }

                        // if there is a tie among faction strength contributions, pick the winner randomly
                        var listOfFactionIds = new List<long>();
                        foreach (var factionContribution in factionStrengthContribution)
                        {
                            if (factionContribution.Value == factionStrengthContribution[asset.WasSeizedByFactionId])
                            {
                                listOfFactionIds.Add(factionContribution.Key);
                            }
                        }

                        asset.WasSeizedByFactionId = listOfFactionIds.Skip(new Random().Next(listOfFactionIds.Count)).First();

                        Service.SetWasSeizedByFactionId(asset.Id, asset.WasSeizedByFactionId);
                    }
                }

                // if the action had Investigate, gain # profiles = asset value
                if (actionSpecializationAccess[action.Id].Contains(Asset.Specialziations.Investigate.ToString()))
                {
                    asset.InfiltrateBy(infiltratingFactionIds, Math.Min(asset.Value, strength - defense));
                }
                else
                {
                    // otherwise, attacking factions each gain 1 profile from this asset
                    asset.InfiltrateBy(infiltratingFactionIds, 1);
                }
            }
            else
            {
                // attackers are compromized unless they have Vanish
                if (!actionSpecializationAccess[action.Id].Contains(Asset.Specialziations.Vanish.ToString()))
                {
                    action.ChangeValueOfCollaborators(strength - defense, perspective);
                }
            }
        }
    }

    private static void InfiltrateBy(this Asset asset, List<long> infiltratingFactionIds, int profileCount)
    {
        foreach (var factionId in infiltratingFactionIds)
        {
            var infiltration = new Infiltration()
            {
                FactionId = factionId,
                ProfileCount = profileCount,
            };

            Service.AddInfiltration(asset.Id, infiltration);
        }
    }

    public static string RenderSuccess(this Asset asset, long perspective)
    {
        var result = string.Empty;

        var strength = asset.ActionStrength(perspective);
        var order = Service.GetOrderOfAssetFromPerspective(perspective, asset.Id);        
        var target = Service.GetAsset(order.TargetId);
        var defense = target.Defense(perspective);

        if (order.Action.Equals(Order.Actions.Damage.ToString()) || order.Action.Equals(Order.Actions.Infiltrate.ToString()))
        {
            var MoS = strength - defense;

            if (MoS >= target.Value)
            {
                result = string.Format("<span style='color:lime'>exceptional success (MoS = {0})</span>", MoS);
            }
            else if (MoS > 0)
            {
                result = string.Format("<span style='color:green'>success (MoS = {0})</span>", MoS);
            }
            else
            {
                result = string.Format("<span style='color:red'>failure (MoS = {0})</span>", MoS);
            }
        }
        else
        {
            // thwarted by Damage?
            var MoS = strength - target.Value;

            if (MoS > 0)
            {
                result = string.Format("<span style='color:green'>exceptional success (defense +{0})</span>", strength);
            }
            else
            {
                result = string.Format("<span style='color:green'>success (defense +{0})</span>", strength);
            }
        }

        return result;
    }

    public static string ResultOfAction(this Asset asset, long perspective)
    {
        asset = Service.GetAsset(asset.Id);
        var trueOrder = Service.GetOrderOfAssetFromPerspective(perspective, asset.Id);
        var faction = Service.GetFactionOfAsset(asset);
        var game = Service.GetGameOfFaction(faction);

        var visibleAssetsFromPerspective = Service.GetAssetsFromPerspective(game, perspective, asset.Turn);
        var visibleAssetIdsFromPerspective = visibleAssetsFromPerspective.Select(x => x.Id);
        var collaboratingAssetIds = Service.GetCollaboratingAssetIds(asset, perspective);
        var collaborators = string.Empty;
        var results = string.Empty;
        var target = Service.GetAsset(trueOrder.TargetId);

        if (visibleAssetIdsFromPerspective.Contains(trueOrder.TargetId))
        {
            if (target.ValueLossFromDamage >= target.Value)
            {
                if (target.Value > 0)
                {
                    results += String.Format("<li>{0} was destroyed.</li>", target.Name);
                }
                else if (!target.WasEnhanced)
                {
                    results += String.Format("<li>{0} was not created.</li>", target.Name);
                }
            }
            else if (target.ValueLossFromDamage > 0)
            {
                results += String.Format("<li>{0} lost {1} value.</li>", target.Name, target.ValueLossFromDamage);
            }

            if (target.WasShrouded)
            {
                results += String.Format("<li>{0} was rendered covert and all profiles on it were lost.</li>", target.Name);
            }

            if (target.WasEnhanced)
            {
                results += String.Format("<li>{0} had its value increased.</li>", target.Name);
            }

            if (!string.IsNullOrEmpty(target.WasInfusedWith))
            {
                results += String.Format("<li>{0} was infused with {1}.</li>", target.Name, target.WasInfusedWith);
            }

            if (!string.IsNullOrEmpty(target.WasPropagatedWith))
            {
                results += String.Format("<li>{0} was propagated with {1}.</li>", target.Name, target.WasPropagatedWith);
            }

            if (target.WasSeizedByFactionId > 0)
            {
                results += String.Format("<li>{0} was seized.</li>", target.Name);
            }
        }

        if (target != null)
        {
            var infiltrationIds = Service.GetInfiltrationIdsOnAssetFromPerspective(game, asset, perspective);

            foreach (var infiltrationId in infiltrationIds)
            {
                var infiltration = Service.GetInfiltration(infiltrationId);
                var infiltratingFaction = Service.GetFaction(infiltration.FactionId);
                var targetFaction = Service.GetFactionOfAsset(target);

                results += String.Format("<li>{0} gained one or more profiles on {1}.</li>",
                    infiltratingFaction.Name,
                    targetFaction.Name
                    );
            }
        }

        foreach (var collaboratorId in collaboratingAssetIds)
        {
            var collaborator = Service.GetAsset(collaboratorId);

            if (visibleAssetIdsFromPerspective.Contains(collaborator.Id))
            {
                if (Service.GetOrderOfAssetFromPerspective(perspective, collaborator.Id).GoingPublic)
                {
                    results += String.Format("<li>{0} went public.</li>", collaborator.Name);
                }

                if (collaborator.ValueIncreaseFromDamage > 0)
                {
                    results += String.Format("<li>{0} gained {1} value.</li>", collaborator.Name, collaborator.ValueIncreaseFromDamage);
                }

                if (collaborator.ValueLossFromCompromised > 0)
                {
                    if (collaborator.ValueLossFromCompromised >= collaborator.Value)
                    {
                        results += String.Format("<li>{0} was destroyed.</li>", collaborator.Name);
                    }
                    else
                    {
                        results += String.Format("<li>{0} lost {1} value.</li>", collaborator.Name, collaborator.ValueLossFromCompromised);
                    }
                }
            }
        }

        return string.Format("{0}<ul>{1}</ul>", asset.GetOrder(perspective, true), results);
    }
}

public class AssetStrengthDescendingComparer : IComparer<Asset>
{
    private long _perspective;

    public AssetStrengthDescendingComparer(long perspective)
    {
        _perspective = perspective;
    }

    int IComparer<Asset>.Compare(Asset x, Asset y)
    {
        return y.ActionStrength(_perspective).CompareTo(x.ActionStrength(_perspective));
    }
}