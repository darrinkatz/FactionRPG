using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class PerspectiveManager
{
    private Perspective _perspective;

    public PerspectiveManager(Perspective perspective)
    {
        // TODO: ensure that the actions only include assets this faction has permission to change
        // eg. prevent hacking of assigning orders to assets belonging to other players

        this._perspective = perspective;
    }

    public Perspective UpdateAssets()
    {
        ResetResults();

        // for each asset, determine result of actions targeting it
        foreach (var scene in _perspective.Scenes)
        {
            var target = GetActor(scene.TargetId);
            int defense = GetDefense(target);
            bool wasDamaged = false;

            foreach (var damageAction in scene.Troupes
                .Where(t => t.Action.Equals(Order.Actions.Damage.ToString())))
            {
                int strength = damageAction.Strength;

                // if the action was successful
                if (strength > defense)
                {
                    wasDamaged = true;

                    // if this attack was an exceptional success
                    if (strength - defense >= target.Value)
                    {
                        // mark it as destroyed
                        target.ValueLossFromDamage = target.Value;

                        // attackers gain value for destroying an asset
                        if (target.Value > 1)
                        {
                            ChangeValueOfCollaborators(GetTroupeActors(damageAction), target.Value - 1);
                        }
                    }
                    else
                    {
                        // reduce its value
                        target.ValueLossFromDamage = strength - defense;

                        // attackers gain value if they have Salvage
                        if (damageAction.SpecializationAccess.Contains(Asset.Specialziations.Salvage.ToString()))
                        {
                            if (target.Value > 1)
                            {
                                ChangeValueOfCollaborators(GetTroupeActors(damageAction), strength - defense - 1);
                            }
                        }
                    }
                }
                else
                {
                    // attackers are compromized unless they have Maneuver
                    if (!damageAction.SpecializationAccess.Contains(Asset.Specialziations.Maneuver.ToString()))
                    {
                        ChangeValueOfCollaborators(GetTroupeActors(damageAction), strength - defense);
                    }
                }
            }

            foreach (var shroudAction in scene.Troupes
                .Where(t => t.Action.Equals(Order.Actions.Shroud.ToString())))
            {
                int strength = shroudAction.Strength;

                // if defensive actions have not been thwarted on this asset
                if (!wasDamaged)
                {
                    // if the action was exceptionally successful
                    if (strength > target.Value)
                    {
                        target.WasShrouded = true;

                        // collaborating factions get a profile on this asset next turn
                        foreach (var collaboratingFactionId in GetTroupeActors(shroudAction).Select(a => a.FactionId))
                        {
                            target.ShroudedByFactionIds += collaboratingFactionId + ",";
                        }
                    }
                }
            }

            foreach (var enhanceAction in scene.Troupes
                .Where(t => t.Action.Equals(Order.Actions.Enhance.ToString())))
            {
                int strength = enhanceAction.Strength;

                // if defensive actions have not been thwarted on this asset
                if (!wasDamaged)
                {
                    // if the action was exceptionally successful
                    if (strength > target.Value)
                    {
                        target.WasEnhanced = true;
                        target.WasInfusedWith = enhanceAction.InfuseWith;
                        target.WasPropagatedWith = enhanceAction.PropagateWith;
                    }
                }
            }

            foreach (var infiltrateAction in scene.Troupes
                .Where(t => t.Action.Equals(Order.Actions.Infiltrate.ToString())))
            {
                int strength = infiltrateAction.Strength;

                if (strength > defense)
                {
                    // make list of collaborating factions and their contributions
                    var infiltratingFactionIds = new List<long>();
                    var factionStrengthContribution = new Dictionary<long, int>();

                    foreach (var collaboratingAssetId in infiltrateAction.ActorIds)
                    {
                        var collaboratingAsset = GetActor(collaboratingAssetId);

                        if (!infiltratingFactionIds.Contains(collaboratingAsset.FactionId))
                        {
                            infiltratingFactionIds.Add(collaboratingAsset.FactionId);
                        }

                        if (infiltrateAction.ShotInTheDarkFactionId == 0)
                        {
                            var hasDisguise = infiltrateAction.SpecializationAccess.Contains(Asset.Specialziations.Disguise.ToString());
                            var contribution = !collaboratingAsset.Covert || hasDisguise ? collaboratingAsset.Value : collaboratingAsset.Value - 1;

                            if (factionStrengthContribution.ContainsKey(collaboratingAsset.FactionId))
                            {
                                factionStrengthContribution[collaboratingAsset.FactionId] += contribution;
                            }
                            else
                            {
                                factionStrengthContribution.Add(collaboratingAsset.FactionId, contribution);
                            }
                        }
                    }

                    // if this attack was an exceptional success
                    if (strength - defense >= target.Value)
                    {
                        if (factionStrengthContribution.Count > 0)
                        {
                            // the faction that contributed the most value to the attack seizes control
                            foreach (var factionContribution in factionStrengthContribution)
                            {
                                if (factionContribution.Value > 0)
                                {
                                    if (target.WasSeizedByFactionId == 0
                                        || factionContribution.Value > factionStrengthContribution[target.WasSeizedByFactionId])
                                    {
                                        target.WasSeizedByFactionId = factionContribution.Key;
                                    }
                                }
                            }

                            // if there is a tie among faction strength contributions, pick the winner randomly
                            var listOfFactionIds = new List<long>();
                            foreach (var factionContribution in factionStrengthContribution)
                            {
                                if (factionContribution.Value == factionStrengthContribution[target.WasSeizedByFactionId])
                                {
                                    listOfFactionIds.Add(factionContribution.Key);
                                }
                            }

                            target.WasSeizedByFactionId = listOfFactionIds.Skip(new Random().Next(listOfFactionIds.Count)).First();
                        }
                    }

                    // if the action had Investigate, gain # profiles = asset value
                    if (infiltrateAction.SpecializationAccess.Contains(Asset.Specialziations.Investigate.ToString()))
                    {
                        InfiltrateAssetByFactions(target, infiltratingFactionIds, Math.Min(target.Value, strength - defense));
                    }
                    else
                    {
                        // otherwise, attacking factions each gain 1 profile from this asset
                        InfiltrateAssetByFactions(target, infiltratingFactionIds, 1);
                    }
                }
                else
                {
                    // attackers are compromized unless they have Vanish
                    if (!infiltrateAction.SpecializationAccess.Contains(Asset.Specialziations.Vanish.ToString()))
                    {
                        ChangeValueOfCollaborators(GetTroupeActors(infiltrateAction), strength - defense);
                    }
                }
            }
        }

        // sort each troupe by action alphabetically
        foreach (var scene in _perspective.Scenes)
        {
            scene.Troupes = scene.Troupes.OrderBy(t => t.Action).ToList();
        }

        return _perspective;
    }

    private void ResetResults()
    {
        // for each asset, reset all calculated results
        foreach (var actor in this._perspective.Actors)
        {
            actor.ValueLossFromCompromised = 0;
            actor.ValueLossFromDamage = 0;
            actor.ValueIncreaseFromDamage = 0;
            actor.ShroudedByFactionIds = string.Empty;
            actor.WasShrouded = false;
            actor.WasEnhanced = false;
            actor.WasInfusedWith = string.Empty;
            actor.WasPropagatedWith = string.Empty;
            actor.WasSeizedByFactionId = 0;
        }
    }

    private Perspective.Actor GetActor(long actorId)
    {
        return this._perspective.Actors.FirstOrDefault(a => a.Id == actorId);
    }

    private int GetDefense(Perspective.Actor actor)
    {
        var result = actor.Value;

        foreach (var action in this._perspective.Scenes.SingleOrDefault(s => s.TargetId == actor.Id).Troupes)
        {
            if (action.Action.Equals(Order.Actions.Enhance.ToString())
                || action.Action.Equals(Order.Actions.Shroud.ToString()))
            {
                if (action.SpecializationAccess.Contains(Asset.Specialziations.Ambush.ToString())
                    && action.Action.Equals(Order.Actions.Shroud.ToString()))
                {
                    result += 2 * action.Strength;
                }
                else
                {
                    result += action.Strength;
                }
            }
        }

        return result;
    }

    private void InfiltrateAssetByFactions(Perspective.Actor actor, List<long> infiltratingFactionIds, int profileCount)
    {
        foreach (var factionId in infiltratingFactionIds)
        {
            var infiltration = new Infiltration()
            {
                FactionId = factionId,
                ProfileCount = profileCount,
            };

            actor.Infiltrations.Add(infiltration);
        }
    }

    private List<Perspective.Actor> GetTroupeActors(Perspective.Troupe troupe)
    {
        return this._perspective.Actors.Where(a => troupe.ActorIds.Contains(a.Id)).ToList();
    }

    private void ChangeValueOfCollaborators(List<Perspective.Actor> actors, int change)
    {
        if (change != 0)
        {
            var weightedList = new List<Perspective.Actor>();
            int totalValue = 0;

            foreach (var collaborator in actors)
            {
                for (int i = 0; i < collaborator.Value; i++)
                {
                    weightedList.Add(collaborator);
                }

                totalValue += collaborator.Value;
            }

            if (change <= totalValue * -1)
            {
                foreach (var collaborator in actors)
                {
                    collaborator.ValueLossFromCompromised = collaborator.Value;
                    collaborator.ExpectedValueChange = string.Format("-{0}", collaborator.Value);
                }
            }
            else
            {
                // calculate percentage chance of value change
                foreach (var collaborator in actors)
                {
                    collaborator.ExpectedValueChange = (((double)collaborator.Value / totalValue) * change).ToString();
                }

                // randomly assign actual value changes
                for (int i = 0; i < Math.Abs(change); i++)
                {
                    // randomly select an index from list
                    var collaborator = weightedList[new Random().Next(weightedList.Count())];

                    // increase or decrease value
                    if (change > 0)
                    {
                        collaborator.ValueIncreaseFromDamage++;
                    }
                    else
                    {
                        collaborator.ValueLossFromCompromised++;

                        // if this asset has been destroyed, don't pick it again
                        if (collaborator.ValueLossFromCompromised == collaborator.Value)
                        {
                            weightedList.RemoveAll(a => a.Id == collaborator.Id);
                        }
                    }
                }
            }
        }
    }
}