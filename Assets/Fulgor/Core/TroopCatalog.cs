using System;

namespace Fulgor.Core.Combat
{
    public enum TroopProfile { Spearmen, MenAtArms, Crossbowmen, LightCavalry }

    public sealed class TroopProfileDefinition
    {
        internal TroopProfileDefinition(string name, FormationKind kind, int quantity, FormationAttributes attributes)
        { Name = name; Kind = kind; DefaultQuantity = quantity; Attributes = attributes; }
        public string Name { get; }
        public FormationKind Kind { get; }
        public int DefaultQuantity { get; }
        public FormationAttributes Attributes { get; }
    }

    /// <summary>Initial numerical profiles migrated from the historical v0.5 catalogue.</summary>
    public static class TroopCatalog
    {
        private static readonly TroopProfileDefinition[] Profiles = {
            new TroopProfileDefinition("Lanceros concejiles", FormationKind.Infantry, 50,
                new FormationAttributes(4, 6, 3, 5, 10, 2, 3, 1, 70, 65)),
            new TroopProfileDefinition("Hombres de armas a pie", FormationKind.Infantry, 40,
                new FormationAttributes(7, 7, 5, 8, 14, 2, 4, 1, 70, 80)),
            new TroopProfileDefinition("Ballesteros de mesnada", FormationKind.Ranged, 30,
                new FormationAttributes(6, 3, 4, 7, 8, 2, 5, 3, 70, 70)),
            new TroopProfileDefinition("Jinetes ligeros", FormationKind.Cavalry, 25,
                new FormationAttributes(6, 4, 4, 6, 12, 4, 7, 1, 70, 60)) };

        public static TroopProfileDefinition Get(TroopProfile profile)
        {
            var index = (int)profile;
            if (index < 0 || index >= Profiles.Length) throw new ArgumentOutOfRangeException(nameof(profile));
            return Profiles[index];
        }
    }
}
