using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class Perspective
{
    public string GameName { get; private set; }
    public List<Scene> Scenes { get; private set; }
    public List<Actor> Actors { get; private set; }

    public Perspective()
    {
        this.Scenes = new List<Scene>();
        this.Actors = new List<Actor>();
    }

    public void Load(long userFactionId, Game game, int turn)
    {
        this.GameName = game.Name;

        var visibleAssets = Service.GetAssetsFromPerspective(game, userFactionId, turn);
        foreach (var asset in visibleAssets)
        {
            this.AddActorToScene(userFactionId, asset);
        }
    }

    public void ChangeOrder(long userFactionId, long actorId, long targetId, string action)
    {
        // update the database
        var order = Service.GetOrderOfAssetFromPerspective(userFactionId, actorId);
        order.TargetId = targetId;
        order.Action = action;
        Service.AddOrReplaceOrder(userFactionId, order);

        var emptyScenes = new List<Scene>();
        var emptyTroupes = new List<Troupe>();

        // remove the actor from previous troupe
        foreach (var scene in this.Scenes)
        {
            foreach (var troupe in scene.Troupes)
            {
                if (troupe.ActorIds.Contains(actorId))
                {
                    RemoveActorFromTroupe(userFactionId, troupe, actorId);

                    if (troupe.ActorIds.Count == 0)
                    {
                        emptyTroupes.Add(troupe);
                    }
                }
            }
        }

        // remove empty troupes
        foreach (var scene in this.Scenes)
        {
            foreach (var troupe in emptyTroupes)
            {
                scene.Troupes.Remove(troupe);

                if (scene.Troupes.Count == 0)
                {
                    emptyScenes.Add(scene);
                }
            }
        }

        // remove empty scenes
        foreach (var scene in emptyScenes)
        {
            this.Scenes.Remove(scene);
        }

        // add actor to new troupe
        AddActorToTroupe(userFactionId, this.Actors.FirstOrDefault(a => a.Id == actorId), order);
    }

    public void AddActorToScene(long userFactionId, Asset asset)
    {
        var faction = Service.GetFactionOfAsset(asset);
        var order = Service.GetOrderOfAssetFromPerspective(userFactionId, asset.Id);

        var actorName = Microsoft.Security.Application.AntiXss.GetSafeHtmlFragment(asset.Name);
        var actorImageUrl = Microsoft.Security.Application.AntiXss.GetSafeHtmlFragment(asset.ImageUrl);

        var actor = new Perspective.Actor()
        {
            Id = asset.Id,
            FactionId = faction.Id,
            FactionName = faction.Name,
            FactionHexColour = faction.HexColour,
            Turn = asset.Turn,
            Value = asset.Value,
            Name = actorName,
            Covert = asset.Covert,
            ImageUrl = actorImageUrl,
            IsSentinel = order.IsSentinel,

            // Specializations
            HasAmbush = asset.HasAmbush,
            HasDisguise = asset.HasDisguise,
            HasInfuse = asset.HasInfuse,
            HasInvestigate = asset.HasInvestigate,
            HasManeuver = asset.HasManeuver,
            HasSalvage = asset.HasSalvage,
            HasPropagate = asset.HasPropagate,
            HasVanish = asset.HasVanish,

            // Infusions
            InfusedWithAmbush = asset.InfusedWithAmbush,
            InfusedWithSalvage = asset.InfusedWithSalvage,
            InfusedWithDisguise = asset.InfusedWithDisguise,
            InfusedWithVanish = asset.InfusedWithVanish,
            InfusedWithManeuver = asset.InfusedWithManeuver,
            InfusedWithInfuse = asset.InfusedWithInfuse,
            InfusedWithInvestigate = asset.InfusedWithInvestigate,
            InfusedWithPropagate = asset.InfusedWithPropagate,

            // Result Data
            ValueLossFromCompromised = asset.ValueLossFromCompromised,
            ValueLossFromDamage = asset.ValueLossFromDamage,
            ValueIncreaseFromDamage = asset.ValueIncreaseFromDamage,
            ExpectedValueChange = asset.ExpectedValueChange,
            ShroudedByFactionIds = asset.ShroudedByFactionIds,
            WasShrouded = asset.WasShrouded,
            WasEnhanced = asset.WasEnhanced,
            WasInfusedWith = asset.WasInfusedWith,
            WasPropagatedWith = asset.WasPropagatedWith,
            WasSeizedByFactionId = asset.WasSeizedByFactionId,
        };

        this.Actors.Add(actor);

        AddActorToTroupe(userFactionId, actor, order);
    }

    private void AddActorToTroupe(long userFactionId, Actor actor, Order order)
    {
        Troupe troupe = null;

        var scene = this.Scenes.FirstOrDefault(s => s.TargetId == order.TargetId);

        if (scene == null)
        {
            scene = new Scene();
            scene.Load(order.TargetId);
            troupe = new Troupe();
            troupe.Load(order.Action);
            troupe.ActorIds.Add(actor.Id);
            scene.Troupes.Add(troupe);
            this.Scenes.Add(scene);
        }
        else
        {
            troupe = scene.Troupes.FirstOrDefault(t => t.Action.Equals(order.Action));

            if (troupe == null)
            {
                troupe = new Troupe();
                troupe.Load(order.Action);
                troupe.ActorIds.Add(actor.Id);
                scene.Troupes.Add(troupe);
            }
            else
            {
                troupe.ActorIds.Add(actor.Id);
            }
        }

        SetTroupeSpecializationAccess(troupe);
        SetTroupeStrength(userFactionId, troupe);
    }

    private void RemoveActorFromTroupe(long userFactionId, Troupe troupe, long actorId)
    {
        troupe.ActorIds.Remove(actorId);
        SetTroupeSpecializationAccess(troupe);
        SetTroupeStrength(userFactionId, troupe);
    }

    private void SetTroupeStrength(long userFactionId, Troupe troupe)
    {
        troupe.Strength = 0;

        foreach (var actorId in troupe.ActorIds)
        {
            var actor = this.Actors.FirstOrDefault(a => a.Id == actorId);
            var order = Service.GetOrderOfAssetFromPerspective(userFactionId, actorId);

            // covert 0-value assets contribute a minimum strength of 0
            if (actor.Value == 0
                || !actor.Covert
                || order.GoingPublic
                || troupe.SpecializationAccess.Contains(Asset.Specialziations.Disguise.ToString())
                )
            {
                troupe.Strength += actor.Value;
            }
            else
            {
                troupe.Strength += actor.Value - 1;
            }
        }
    }

    private void SetTroupeSpecializationAccess(Troupe troupe)
    {
        troupe.SpecializationAccess = new List<string>();

        foreach (var actorId in troupe.ActorIds)
        {
            var actor = this.Actors.FirstOrDefault(a => a.Id == actorId);

            if (actor.HasAmbush || actor.InfusedWithAmbush) troupe.SpecializationAccess.Add(Asset.Specialziations.Ambush.ToString());
            if (actor.HasDisguise || actor.InfusedWithDisguise) troupe.SpecializationAccess.Add(Asset.Specialziations.Disguise.ToString());
            if (actor.HasInfuse || actor.InfusedWithInfuse) troupe.SpecializationAccess.Add(Asset.Specialziations.Infuse.ToString());
            if (actor.HasInvestigate || actor.InfusedWithInvestigate) troupe.SpecializationAccess.Add(Asset.Specialziations.Investigate.ToString());
            if (actor.HasManeuver || actor.InfusedWithManeuver) troupe.SpecializationAccess.Add(Asset.Specialziations.Maneuver.ToString());
            if (actor.HasPropagate || actor.InfusedWithPropagate) troupe.SpecializationAccess.Add(Asset.Specialziations.Propagate.ToString());
            if (actor.HasSalvage || actor.InfusedWithSalvage) troupe.SpecializationAccess.Add(Asset.Specialziations.Salvage.ToString());
            if (actor.HasVanish || actor.InfusedWithVanish) troupe.SpecializationAccess.Add(Asset.Specialziations.Vanish.ToString());
        }
    }

    public class Scene
    {
        public long TargetId { get; private set; }

        public List<Troupe> Troupes { get; set; }

        public Scene()
        {
            this.Troupes = new List<Troupe>();
        }

        public void Load(long targetId)
        {
            this.TargetId = targetId;
        }
    }

    public class Troupe
    {
        public string Action { get; private set; }
        public List<long> ActorIds { get; private set; }

        // order details
        public string InfuseWith { get; private set; }
        public string PropagateWith { get; private set; }
        public long ShotInTheDarkFactionId { get; private set; }

        // derived properties
        public int Strength { get; set; }
        public List<string> SpecializationAccess { get; set; }

        public Troupe()
        {
            this.ActorIds = new List<long>();
            this.SpecializationAccess = new List<string>();
        }

        public void Load(string action)
        {
            this.Action = action;
            this.Strength = 0;
        }
    }

    public class Actor
    {
        // Properties
        public long Id { get; set; }
        public long FactionId { get; set; }
        public string FactionName { get; set; }
        public string FactionHexColour { get; set; }
        public int Turn { get; set; }
        public int Value { get; set; }
        public string Name { get; set; }
        public bool Covert { get; set; }
        public string ImageUrl { get; set; }
        public bool IsSentinel { get; set; }

        public List<Infiltration> Infiltrations { get; set; }

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

        public Actor()
        {
            this.Infiltrations = new List<Infiltration>();
        }
    }
}