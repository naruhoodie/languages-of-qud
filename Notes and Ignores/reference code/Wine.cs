using System.Collections.Generic;
using System.Text;
using System;
using XRL.World.Parts;
using XRL.World;

namespace XRL.Liquids
{

    [IsLiquid]
    [Serializable]
    public class LiquidWine : BaseLiquid
    {

        new public const string ID = "wine";

        [NonSerialized] public static List<string> Colors = new List<string>(2){ "m", "r" };

        public LiquidWine() : base(ID)
        {
            FlameTemperature = 620;
            VaporTemperature = 1620;
            Combustibility = 15;
            ThermalConductivity = 45;
            Fluidity = 30;
            Evaporativity = 3;
            Staining = 2;
            Cleansing = 1;
        }

        public override List<string> GetColors()
        {
            return Colors;
        }

        public override string GetColor()
        {
            return "m";
        }

        public override string GetName(LiquidVolume Liquid)
        {
            return "{{m|wine}}";
        }

        public override string GetAdjective(LiquidVolume Liquid)
        {
            return "{{m|lush}}";
        }

        public override string GetWaterRitualName()
        {
            return "wine";
        }

        public override string GetSmearedAdjective(LiquidVolume Liquid)
        {
            return "{{m|lush}}";
        }

        public override string GetSmearedName(LiquidVolume Liquid)
        {
            return "{{m|lush}}";
        }

        public override string GetStainedName(LiquidVolume Liquid)
        {
            return "{{m|wine}}";
        }

        public override bool Drank(LiquidVolume Liquid, int Volume, GameObject Target, StringBuilder Message, ref bool ExitInterface)
        {
            long CurrentTurn = Core.XRLCore.Core.Game.Turns;
            GameObject GO = Target;
            if (GO == null)
            {
                return true;
            }
            if (Target.HasPart<Stomach>())
            {
                Target.FireEvent(Event.New("AddWater", "Amount", 2 * Volume, "Forced", 1));
                Message.Compound("You flush with the warming draught!");
            }
            if (!GO.HasProperty("ConfuseOnEatTurnWine"))
            {
                GO.SetLongProperty("ConfuseOnEatTurnWine", Core.XRLCore.Core.Game.Turns);
            }
            long LastTurn = GO.GetLongProperty("ConfuseOnEatTurnWine");
            int LastAmount = GO.GetIntProperty("ConfuseOnEatAmountWine");
            if (CurrentTurn - LastTurn > 80)
            {
                LastAmount = 0;
            }
            if (LastAmount > Math.Max(1, GO.StatMod("Toughness") * 2))
            {
                if (GO.ApplyEffect(new World.Effects.Confused(Rules.Stat.Roll("5d5"), 1, 3)))
                {
                    ExitInterface = true;
                }
            }
            GO.SetLongProperty("ConfuseOnEatTurnWine", CurrentTurn);
            LastAmount++;
            GO.SetIntProperty("ConfuseOnEatAmountWine", LastAmount);
            return true;
        }

        public override void RenderBackgroundPrimary(LiquidVolume Liquid, RenderEvent eRender)
        {
            if (eRender.ColorsVisible)
            {
                eRender.ColorString = "^m" + eRender.ColorString;
            }
        }

        public override void BaseRenderPrimary(LiquidVolume Liquid)
        {
            Liquid.ParentObject.Render.ColorString = "&m^r";
            Liquid.ParentObject.Render.TileColor = "&m";
            Liquid.ParentObject.Render.DetailColor = "r";
        }

        public override void BaseRenderSecondary(LiquidVolume Liquid)
        {
            Liquid.ParentObject.Render.ColorString += "&m";
        }

        public override void RenderPrimary(LiquidVolume Liquid, RenderEvent eRender)
        {
            if (!Liquid.IsWadingDepth())
            {
                return;
            }
            if (Liquid.ParentObject.IsFrozen())
            {
                eRender.RenderString = "~";
                eRender.TileVariantColors("&M^b", "&M", "b");
            }
            else
            {
                Render RenderPart = Liquid.ParentObject.Render;

                int nFrame = (Core.XRLCore.CurrentFrame + Liquid.FrameOffset) % 60;

                if (Rules.Stat.RandomCosmetic(1, 600) == 1)
                {
                    eRender.RenderString = "" + (char) 15;
                    eRender.TileVariantColors("&r^m", "&r", "m");
                }
                if (Rules.Stat.RandomCosmetic(1, 60) == 1)
                {
                    RenderPart.ColorString = "&m^r";
                    RenderPart.TileColor = "&m";
                    RenderPart.DetailColor = "r";
                    if (nFrame < 15)
                    {
                        RenderPart.RenderString = "" + (char) 247;
                    }
                    else
                    if (nFrame < 30)
                    {
                        RenderPart.RenderString = "~";
                    }
                    else
                    if (nFrame < 45)
                    {
                        RenderPart.RenderString = "" + (char) 9;
                    }
                    else
                    {
                        RenderPart.RenderString = "~";
                    }
                }
            }
        }

        public override void RenderSecondary(LiquidVolume Liquid, RenderEvent eRender)
        {
            if (eRender.ColorsVisible)
            {
                eRender.ColorString += "&m";
            }
        }

        public override void RenderSmearPrimary(LiquidVolume Liquid, RenderEvent eRender, GameObject obj)
        {
            if (eRender.ColorsVisible)
            {
                int nFrame = Core.XRLCore.CurrentFrame % 60;
                if (nFrame > 5 && nFrame < 15)
                {
                    eRender.ColorString = "&m";
                }
            }
            base.RenderSmearPrimary(Liquid, eRender, obj);
        }

        public override float GetValuePerDram()
        {
            return 4f;
        }

    }

}
