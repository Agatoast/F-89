using System;
using F89.Weapons;
using UnityEngine;

namespace F89.Core
{
    public enum AircraftLoadoutWeapon
    {
        None = 0,
        Agm88j = 1,
        Gbu12 = 2,
        Agm114 = 3,
        Aim9z = 4
    }

    public static class AircraftLoadoutState
    {
        public const int MaxLoadoutLbs = 18000;
        public const int MaxGunRounds = 3000;
        public const int GunRoundStep = 1;
        public const int GunRoundWeightLbs = 1;
        public const int LinkedHardpointPairCount = 10;
        public const int PayloadLbsPerSpeedPenaltyStep = 1000;
        public const int SpeedPenaltyPercentPerStep = 1;
        public const int PayloadLbsExemptFromSpeedPenalty = 5000;

        private static readonly int[] WeaponWeightsLbs =
        {
            0,
            1030,
            600,
            100,
            190
        };

        public static int GunRounds { get; private set; } = 300;
        public static bool HasConfiguredLoadout { get; private set; }

        public static AircraftLoadoutWeapon[] LinkedHardpointPairWeapons { get; } = new AircraftLoadoutWeapon[LinkedHardpointPairCount];
        public static AircraftLoadoutWeapon WingTipWeapon { get; private set; }

        public static int GetWeaponWeightLbs(AircraftLoadoutWeapon weapon)
        {
            return WeaponWeightsLbs[(int)weapon];
        }

        public static string GetWeaponLabel(AircraftLoadoutWeapon weapon)
        {
            return weapon switch
            {
                AircraftLoadoutWeapon.Agm88j => "AGM-88J",
                AircraftLoadoutWeapon.Gbu12 => "GBU-12",
                AircraftLoadoutWeapon.Agm114 => "AGM-114",
                AircraftLoadoutWeapon.Aim9z => "AIM-9Z",
                _ => string.Empty
            };
        }

        public static string GetWeaponIconName(AircraftLoadoutWeapon weapon)
        {
            return weapon switch
            {
                AircraftLoadoutWeapon.Agm88j => "agm88j",
                AircraftLoadoutWeapon.Gbu12 => "gbu12",
                AircraftLoadoutWeapon.Agm114 => "agm114",
                AircraftLoadoutWeapon.Aim9z => "aim9z",
                _ => string.Empty
            };
        }

        public static int GetMountedCount(AircraftLoadoutWeapon weapon)
        {
            CountWeapons(out var aim9z, out var agm88j, out var gbu12, out var agm114);
            return weapon switch
            {
                AircraftLoadoutWeapon.Aim9z => aim9z,
                AircraftLoadoutWeapon.Agm88j => agm88j,
                AircraftLoadoutWeapon.Gbu12 => gbu12,
                AircraftLoadoutWeapon.Agm114 => agm114,
                _ => 0
            };
        }

        public static void ResetForNewSortie()
        {
            GunRounds = 300;
            HasConfiguredLoadout = false;
            Array.Clear(LinkedHardpointPairWeapons, 0, LinkedHardpointPairWeapons.Length);
            WingTipWeapon = AircraftLoadoutWeapon.None;
        }

        public static void LoadCharacterDefault(CharacterSaveData save)
        {
            ResetForNewSortie();
            if (save == null || !save.HasDefaultAircraftPayload)
            {
                return;
            }

            var savedPairs = save.DefaultAircraftLinkedPairWeapons;
            for (var i = 0; i < LinkedHardpointPairWeapons.Length; i++)
            {
                var savedWeapon = savedPairs != null && i < savedPairs.Length
                    ? (AircraftLoadoutWeapon)savedPairs[i]
                    : AircraftLoadoutWeapon.None;
                LinkedHardpointPairWeapons[i] = IsValidWeapon(savedWeapon)
                    ? savedWeapon
                    : AircraftLoadoutWeapon.None;
            }

            var wingTip = (AircraftLoadoutWeapon)save.DefaultAircraftWingTipWeapon;
            WingTipWeapon = wingTip == AircraftLoadoutWeapon.Aim9z
                ? wingTip
                : AircraftLoadoutWeapon.None;
            GunRounds = Mathf.Clamp(save.DefaultAircraftGunRounds, 0, MaxGunRounds);
        }

        public static void SaveCharacterDefault(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            var pairs = new int[LinkedHardpointPairWeapons.Length];
            for (var i = 0; i < pairs.Length; i++)
            {
                pairs[i] = (int)LinkedHardpointPairWeapons[i];
            }

            save.HasDefaultAircraftPayload = true;
            save.DefaultAircraftLinkedPairWeapons = pairs;
            save.DefaultAircraftWingTipWeapon = (int)WingTipWeapon;
            save.DefaultAircraftGunRounds = GunRounds;
            CharacterSaveRepository.WriteWorldProgress(save);
        }

        public static void ClearPayload()
        {
            Array.Clear(LinkedHardpointPairWeapons, 0, LinkedHardpointPairWeapons.Length);
            WingTipWeapon = AircraftLoadoutWeapon.None;
            GunRounds = 0;
            HasConfiguredLoadout = false;
        }

        public static void MarkConfigured()
        {
            HasConfiguredLoadout = true;
        }

        public static int ComputeCurrentLoadoutLbs()
        {
            CountWeapons(out var aim9z, out var agm88j, out var gbu12, out var agm114);
            return ComputePayloadLbs(aim9z, agm88j, gbu12, agm114, GunRounds);
        }

        public static int ComputePayloadLbs(int aim9z, int agm88j, int gbu12, int agm114, int gunRounds)
        {
            return Mathf.Max(0, gunRounds) * GunRoundWeightLbs
                + GetWeaponWeightLbs(AircraftLoadoutWeapon.Aim9z) * Mathf.Max(0, aim9z)
                + GetWeaponWeightLbs(AircraftLoadoutWeapon.Agm88j) * Mathf.Max(0, agm88j)
                + GetWeaponWeightLbs(AircraftLoadoutWeapon.Gbu12) * Mathf.Max(0, gbu12)
                + GetWeaponWeightLbs(AircraftLoadoutWeapon.Agm114) * Mathf.Max(0, agm114);
        }

        public static int ComputeSpeedDecreasePercent()
        {
            return ComputeSpeedDecreasePercentFromLbs(ComputeCurrentLoadoutLbs());
        }

        public static int ComputeSpeedDecreasePercentFromLbs(int payloadLbs)
        {
            var penalizedLbs = Mathf.Max(0, payloadLbs - PayloadLbsExemptFromSpeedPenalty);
            if (penalizedLbs <= 0)
            {
                return 0;
            }

            return Mathf.CeilToInt(penalizedLbs / (float)PayloadLbsPerSpeedPenaltyStep) * SpeedPenaltyPercentPerStep;
        }

        public static float ComputeMaxAirspeedMultiplier()
        {
            return ComputeMaxAirspeedMultiplierFromLbs(ComputeCurrentLoadoutLbs());
        }

        public static float ComputeMaxAirspeedMultiplierFromLbs(int payloadLbs)
        {
            return Mathf.Max(0f, 1f - ComputeSpeedDecreasePercentFromLbs(payloadLbs) / 100f);
        }

        public static bool CanAssignToLinkedPair(int pairIndex, AircraftLoadoutWeapon weapon)
        {
            if (pairIndex < 0 || pairIndex >= LinkedHardpointPairWeapons.Length)
            {
                return false;
            }

            return !WouldExceedMaxLoadoutForLinkedPair(pairIndex, weapon);
        }

        public static bool CanAssignToWingTip(AircraftLoadoutWeapon weapon)
        {
            if (weapon != AircraftLoadoutWeapon.None && weapon != AircraftLoadoutWeapon.Aim9z)
            {
                return false;
            }

            return !WouldExceedMaxLoadoutForWingTip(weapon);
        }

        public static bool WouldExceedMaxLoadoutForLinkedPair(int pairIndex, AircraftLoadoutWeapon weapon)
        {
            if (pairIndex < 0 || pairIndex >= LinkedHardpointPairWeapons.Length)
            {
                return false;
            }

            var delta = GetWeaponWeightLbs(weapon) * 2 - GetWeaponWeightLbs(LinkedHardpointPairWeapons[pairIndex]) * 2;
            return ComputeCurrentLoadoutLbs() + delta > MaxLoadoutLbs;
        }

        public static bool WouldExceedMaxLoadoutForWingTip(AircraftLoadoutWeapon weapon)
        {
            if (weapon != AircraftLoadoutWeapon.None && weapon != AircraftLoadoutWeapon.Aim9z)
            {
                return false;
            }

            var delta = GetWeaponWeightLbs(weapon) * 2 - GetWeaponWeightLbs(WingTipWeapon) * 2;
            return ComputeCurrentLoadoutLbs() + delta > MaxLoadoutLbs;
        }

        public static void AssignLinkedPair(int pairIndex, AircraftLoadoutWeapon weapon)
        {
            if (pairIndex < 0 || pairIndex >= LinkedHardpointPairWeapons.Length)
            {
                return;
            }

            LinkedHardpointPairWeapons[pairIndex] = weapon;
        }

        public static void AssignWingTip(AircraftLoadoutWeapon weapon)
        {
            WingTipWeapon = weapon;
        }

        public static AircraftLoadoutWeapon GetLinkedPairWeapon(int pairIndex)
        {
            return pairIndex >= 0 && pairIndex < LinkedHardpointPairWeapons.Length
                ? LinkedHardpointPairWeapons[pairIndex]
                : AircraftLoadoutWeapon.None;
        }

        public static void AdjustGunRounds(int delta)
        {
            var newRounds = Mathf.Clamp(GunRounds + delta, 0, MaxGunRounds);
            var weightDelta = (newRounds - GunRounds) * GunRoundWeightLbs;
            if (ComputeCurrentLoadoutLbs() + weightDelta > MaxLoadoutLbs)
            {
                return;
            }

            GunRounds = newRounds;
        }

        public static bool TrySetGunRounds(int rounds)
        {
            var newRounds = Mathf.Clamp(rounds, 0, MaxGunRounds);
            var weightDelta = (newRounds - GunRounds) * GunRoundWeightLbs;
            if (ComputeCurrentLoadoutLbs() + weightDelta > MaxLoadoutLbs)
            {
                return false;
            }

            GunRounds = newRounds;
            return true;
        }

        public static void ApplyToWeaponController(PlayerWeaponController weapons)
        {
            if (weapons == null || !HasConfiguredLoadout)
            {
                return;
            }

            CountWeapons(out var aim9z, out var agm88j, out var gbu12, out var agm114);
            weapons.ApplySortieLoadout(aim9z, agm88j, gbu12, agm114, GunRounds);
        }

        private static void CountWeapons(out int aim9z, out int agm88j, out int gbu12, out int agm114)
        {
            aim9z = 0;
            agm88j = 0;
            gbu12 = 0;
            agm114 = 0;

            for (var i = 0; i < LinkedHardpointPairWeapons.Length; i++)
            {
                CountSingleWeapon(LinkedHardpointPairWeapons[i], mirrored: true, ref aim9z, ref agm88j, ref gbu12, ref agm114);
            }

            CountSingleWeapon(WingTipWeapon, mirrored: true, ref aim9z, ref agm88j, ref gbu12, ref agm114);
        }

        private static bool IsValidWeapon(AircraftLoadoutWeapon weapon)
        {
            return weapon >= AircraftLoadoutWeapon.None && weapon <= AircraftLoadoutWeapon.Aim9z;
        }

        private static void CountSingleWeapon(
            AircraftLoadoutWeapon weapon,
            bool mirrored,
            ref int aim9z,
            ref int agm88j,
            ref int gbu12,
            ref int agm114)
        {
            var multiplier = mirrored ? 2 : 1;
            switch (weapon)
            {
                case AircraftLoadoutWeapon.Aim9z:
                    aim9z += multiplier;
                    break;
                case AircraftLoadoutWeapon.Agm88j:
                    agm88j += multiplier;
                    break;
                case AircraftLoadoutWeapon.Gbu12:
                    gbu12 += multiplier;
                    break;
                case AircraftLoadoutWeapon.Agm114:
                    agm114 += multiplier;
                    break;
            }
        }
    }
}
