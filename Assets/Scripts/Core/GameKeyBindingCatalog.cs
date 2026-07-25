using UnityEngine;

namespace F89.Core
{
    public static class GameKeyBindingIds
    {
        public const string TurnLeft = "turn_left";
        public const string TurnRight = "turn_right";
        public const string Throttle = "throttle";
        public const string Airbrake = "airbrake";
        public const string Afterburner = "afterburner";
        public const string Fire = "fire";
        public const string SelectGau27a = "select_gau27a";
        public const string SelectAgm114 = "select_agm114";
        public const string SelectGbu12 = "select_gbu12";
        public const string SelectAgm88j = "select_agm88j";
        public const string SelectAim9z = "select_aim9z";
        public const string Flare = "flare";
        public const string CycleTarget = "cycle_target";
    }

    public readonly struct GameKeyBindingDefinition
    {
        public GameKeyBindingDefinition(string id, string label, KeyCode defaultKey)
        {
            Id = id;
            Label = label;
            DefaultKey = defaultKey;
        }

        public string Id { get; }
        public string Label { get; }
        public KeyCode DefaultKey { get; }
    }

    public static class GameKeyBindingCatalog
    {
        public static readonly GameKeyBindingDefinition[] Bindings =
        {
            new GameKeyBindingDefinition(GameKeyBindingIds.TurnLeft, "Turn Left", KeyCode.A),
            new GameKeyBindingDefinition(GameKeyBindingIds.TurnRight, "Turn Right", KeyCode.D),
            new GameKeyBindingDefinition(GameKeyBindingIds.Throttle, "Throttle", KeyCode.W),
            new GameKeyBindingDefinition(GameKeyBindingIds.Airbrake, "Airbrake", KeyCode.S),
            new GameKeyBindingDefinition(GameKeyBindingIds.Afterburner, "Afterburner", KeyCode.LeftShift),
            new GameKeyBindingDefinition(GameKeyBindingIds.Fire, "Fire Weapon", KeyCode.Mouse0),
            new GameKeyBindingDefinition(GameKeyBindingIds.SelectGau27a, "Select GAU-27A", KeyCode.Alpha1),
            new GameKeyBindingDefinition(GameKeyBindingIds.SelectAgm114, "Select AGM-114", KeyCode.Alpha2),
            new GameKeyBindingDefinition(GameKeyBindingIds.SelectGbu12, "Select GBU-12", KeyCode.Alpha3),
            new GameKeyBindingDefinition(GameKeyBindingIds.SelectAgm88j, "Select AGM-88J", KeyCode.Alpha4),
            new GameKeyBindingDefinition(GameKeyBindingIds.SelectAim9z, "Select AIM-9Z", KeyCode.Alpha5),
            new GameKeyBindingDefinition(GameKeyBindingIds.Flare, "Deploy Flare", KeyCode.F),
            new GameKeyBindingDefinition(GameKeyBindingIds.CycleTarget, "Cycle Target", KeyCode.Tab)
        };

        public static bool TryGetDefinition(string bindingId, out GameKeyBindingDefinition definition)
        {
            foreach (var binding in Bindings)
            {
                if (binding.Id == bindingId)
                {
                    definition = binding;
                    return true;
                }
            }

            definition = default;
            return false;
        }
    }
}
