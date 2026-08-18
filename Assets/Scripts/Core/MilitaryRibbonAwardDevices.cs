namespace F89.Core

{

    public enum RibbonAwardDeviceMetal

    {

        None = 0,

        Bronze = 1,

        Silver = 2,

        Gold = 3

    }



    public readonly struct RibbonDeviceComposition

    {

        public RibbonDeviceComposition(int goldStars, int silverStars, int bronzeStars)

        {

            GoldStars = goldStars;

            SilverStars = silverStars;

            BronzeStars = bronzeStars;

        }



        public int GoldStars { get; }

        public int SilverStars { get; }

        public int BronzeStars { get; }



        public bool HasDevices => GoldStars > 0 || SilverStars > 0 || BronzeStars > 0;

    }



    /// <summary>

    /// Star devices for repeat awards of the same ribbon (tiers 1–31).

    /// Higher metal sits to the wearer's right (viewer's left): gold, then silver, then bronze.

    /// </summary>

    public static class MilitaryRibbonAwardDevices

    {

        public const int MaxTrackedAwards = 31;

        public const int MaxStarsPerMetal = 5;



        public static bool TryResolve(int awardCount, out RibbonDeviceComposition composition)

        {

            composition = default;

            if (awardCount <= 1)

            {

                return false;

            }



            var tier = awardCount > MaxTrackedAwards ? MaxTrackedAwards : awardCount;



            if (tier <= 5)

            {

                composition = new RibbonDeviceComposition(0, 0, tier - 1);

                return composition.BronzeStars > 0;

            }



            if (tier == 6)

            {

                composition = new RibbonDeviceComposition(0, 1, 0);

                return true;

            }



            if (tier <= 10)

            {

                composition = new RibbonDeviceComposition(0, 1, tier - 6);

                return true;

            }



            if (tier == 11)

            {

                composition = new RibbonDeviceComposition(0, 2, 0);

                return true;

            }



            if (tier <= 15)

            {

                composition = new RibbonDeviceComposition(0, 2, tier - 11);

                return true;

            }



            if (tier == 16)

            {

                composition = new RibbonDeviceComposition(0, 3, 0);

                return true;

            }



            if (tier <= 20)

            {

                composition = new RibbonDeviceComposition(0, 3, tier - 16);

                return true;

            }



            if (tier == 21)

            {

                composition = new RibbonDeviceComposition(0, 4, 0);

                return true;

            }



            if (tier <= 25)

            {

                composition = new RibbonDeviceComposition(0, 4, tier - 21);

                return true;

            }



            if (tier == 26)

            {

                composition = new RibbonDeviceComposition(0, 5, 0);

                return true;

            }



            if (tier <= 30)

            {

                composition = new RibbonDeviceComposition(1, 0, tier - 26);

                return true;

            }



            composition = new RibbonDeviceComposition(1, 1, 0);

            return true;

        }

    }

}

