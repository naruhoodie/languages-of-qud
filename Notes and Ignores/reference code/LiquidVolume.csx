using ConsoleLib.Console;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System;
using Occult.Engine.CodeGeneration;
using XRL.Language;
using XRL.Liquids;
using XRL.UI;
using XRL.World.Anatomy;
using XRL.World.Capabilities;
using XRL.World.Effects;
using XRL;
using XRL.Collections;
using UnityEngine.Purchasing;
using PlayFab.ClientModels;
using XRL.Messages;

namespace XRL.World.Parts
{

    [AttributeUsage(AttributeTargets.Class)]
    public class IsLiquid : Attribute
    {

        public IsLiquid()
        {
        }

    }

    [Serializable]
    [GeneratePoolingPartial]
    [GenerateSerializationPartial]
    [HasModSensitiveStaticCache]
    public partial class LiquidVolume : IPart
    {

        public const int SWIM_THRESHOLD = 2000;
        public const int WADE_THRESHOLD = 200;
        public const int CLEANING_MIXTURE_THRESHOLD = 2;

        public const int LIQUID_FLOWING             = 0x00000001;
        public const int LIQUID_COLLECTOR           = 0x00000002;
        public const int LIQUID_SEALED              = 0x00000004;
        public const int LIQUID_MANUAL_SEAL         = 0x00000008;
        public const int LIQUID_VISIBLE_WHEN_SEALED = 0x00000010;
        public const int LIQUID_SHOW_SEAL           = 0x00000020;
        public const int LIQUID_HAS_DRAIN           = 0x00000040;

        public int Flags = LIQUID_SHOW_SEAL;

        [JsonIgnore]
        public bool Flowing // unstable
        {
            get => (Flags & LIQUID_FLOWING) == LIQUID_FLOWING;
            set => Flags = value ? (Flags | LIQUID_FLOWING) : (Flags & ~LIQUID_FLOWING);
        }

        [JsonIgnore]
        public bool Collector
        {
            get => (Flags & LIQUID_COLLECTOR) == LIQUID_COLLECTOR;
            set => Flags = value ? (Flags | LIQUID_COLLECTOR) : (Flags & ~LIQUID_COLLECTOR);
        }

        [JsonIgnore]
        public bool Sealed
        {
            get => (Flags & LIQUID_SEALED) == LIQUID_SEALED;
            set => Flags = value ? (Flags | LIQUID_SEALED) : (Flags & ~LIQUID_SEALED);
        }

        [JsonIgnore]
        public bool ManualSeal
        {
            get => (Flags & LIQUID_MANUAL_SEAL) == LIQUID_MANUAL_SEAL;
            set => Flags = value ? (Flags | LIQUID_MANUAL_SEAL) : (Flags & ~LIQUID_MANUAL_SEAL);
        }

        [JsonIgnore]
        public bool LiquidVisibleWhenSealed
        {
            get => (Flags & LIQUID_VISIBLE_WHEN_SEALED) == LIQUID_VISIBLE_WHEN_SEALED;
            set => Flags = value ? (Flags | LIQUID_VISIBLE_WHEN_SEALED) : (Flags & ~LIQUID_VISIBLE_WHEN_SEALED);
        }

        [JsonIgnore]
        public bool ShowSeal
        {
            get => (Flags & LIQUID_SHOW_SEAL) == LIQUID_SHOW_SEAL;
            set => Flags = value ? (Flags | LIQUID_SHOW_SEAL) : (Flags & ~LIQUID_SHOW_SEAL);
        }

        [JsonIgnore]
        public bool HasDrain
        {
            get => (Flags & LIQUID_HAS_DRAIN) == LIQUID_HAS_DRAIN;
            set => Flags = value ? (Flags | LIQUID_HAS_DRAIN) : (Flags & ~LIQUID_HAS_DRAIN);
        }

        [JsonIgnore]
        public override int Priority => PRIORITY_INTEGRAL;

        [NonSerialized] public int FrameOffset;
        public int MaxVolume = -1; // -1 = No maximum, uncontained, open-air volume ("open volume")
        public int Volume; // Current volume
        public string StartVolume = "";
        public string NamePreposition;
        public string UnknownNamePreposition;
        public string AutoCollectLiquidType;

        public string Primary;
        public string Secondary;

        public string _SmearedName;
        public string _SmearedColor;
        public string _StainedName;
        public string _StainedColor;

        public Dictionary<string, int> ComponentLiquids = new(); // Component liquid makeup is out of 1000

        public static bool isValidLiquid(string id)
        {
            if (Liquids == null || id == null)
            {
                return false;
            }
            return Liquids.ContainsKey(id);
        }

        public static IEnumerable<BaseLiquid> getAllLiquids()
        {
            foreach (var liquid in Liquids.Values)
            {
                yield return liquid;
            }
        }

        [Obsolete("Use GetLiquid(ReadOnlySpan<char>) - Will not be removed before Q1 2025")]
        public static BaseLiquid getLiquid( string id )
        {
            return GetLiquid(id);
        }

        /// <summary>
        ///     Gets the corresponding liquid from the <see cref="Liquids"/> dictionary.
        /// </summary>
        /// <param name="ID">The liquid string ID</param>
        /// <returns>The BaseLiquid instance</returns>
        public static BaseLiquid GetLiquid(ReadOnlySpan<char> ID)
        {
            if (Liquids.TryGetValue(ID, out var result))
            {
                return result;
            }
            return null;
        }

        [ModSensitiveStaticCache][NonSerialized] private static StringMap<BaseLiquid> _Liquids;
        public static StringMap<BaseLiquid> Liquids
        {
            get
            {
                if (_Liquids == null)
                {
                    XRL.UI.Loading.LoadTask("Initialize liquids", Init);
                }
                return _Liquids;
            }
        }

        public LiquidVolume()
        {
        }

        public LiquidVolume(string Liquid, int Volume) : this()
        {
            this.InitialLiquid = Liquid;
            this.Volume = Volume;
        }

        public LiquidVolume(string Liquid, int Volume, int MaxVolume) : this(Liquid, Volume)
        {
            this.MaxVolume = MaxVolume;
        }

        public LiquidVolume(Dictionary<string, int> Amounts)
        {
            foreach (KeyValuePair<string, int> KV in Amounts)
            {
                ComponentLiquids.Add(KV.Key, KV.Value);
                Volume += KV.Value;
            }
            NormalizeProportions();
        }

        public override void Attach()
        {
            ParentObject.LiquidVolume = this;
            FrameOffset = Rules.Stat.RandomCosmetic(0, 60);
        }

        public override void Remove()
        {
            if (ParentObject?.LiquidVolume == this)
            {
                ParentObject.LiquidVolume = null;
            }
        }

        public static GameObject create( List<string> components, int vol = 1000 )
        {
            GameObject ret = GameObjectFactory.create("WaterPool");
            var volume = ret.LiquidVolume;

            volume.Empty(WillCheckImage: true);

            for (int x = 0; x < components.Count; x++)
            {
                string LiquidID = components[x];
                if (volume.ComponentLiquids.ContainsKey(LiquidID))
                {
                    volume.ComponentLiquids[LiquidID] += 1000 / components.Count;
                }
                else
                {
                    volume.ComponentLiquids.Add(LiquidID, 1000 / components.Count);
                }
            }

            volume.Volume = vol;
            volume.Update();

            return ret;
        }

        public bool EffectivelySealed()
        {
            return Sealed && !IsBroken();
        }

        public int GetAdsorbableDrams(GameObject obj)
        {
            return obj.GetMaximumLiquidExposure() * GetLiquidAdsorbence() / 100;
        }

        /// <summary>
        /// If the specified object is in contact with this liquid volume,
        /// calculates how much of a specified liquid the object is exposed to,
        /// in millidrams.
        /// </summary>
        /// <param Name="obj">
        /// the object whose exposure we're checking
        /// </param>
        /// <param Name="Liquid">
        /// the ID of the liquid we're checking exposure to
        /// </param>
        /// <returns>
        /// amount of the liquid the object is exposed to, in millidrams (divide
        /// by 1000 to get drams)
        /// </returns>
        public int GetLiquidExposureMillidrams(GameObject obj, string Liquid)
        {
            int Proportion;
            if (!ComponentLiquids.TryGetValue(Liquid, out Proportion) || Proportion <= 0)
            {
                return 0;
            }
            double Drams = Math.Min(obj.GetMaximumLiquidExposureAsDouble(), Volume);
            if (Drams <= 0)
            {
                return 0;
            }
            return (int) Math.Round(Drams * Proportion);
        }

        public string GetPreparedCookingIngredientLiquidDomainPairs()
        {
            StringBuilder SB = Event.NewStringBuilder();
            foreach (BaseLiquid liquid in GetComponentLiquids())
            {
                string ingredientType = liquid.GetPreparedCookingIngredient();
                if (!ingredientType.IsNullOrEmpty())
                {
                    foreach (string part in ingredientType.CachedCommaExpansion())
                    {
                        if (SB.Length != 0)
                        {
                            SB.Append(',');
                        }
                        SB
                            .Append(liquid.ID)
                            .Append(':')
                            .Append(part)
                        ;
                    }
                }
            }
            return SB.ToString();
        }

        public bool HasPreparedCookingIngredient()
        {
            if (Volume == 0)
            {
                return false;
            }
            foreach (BaseLiquid liquid in GetComponentLiquids())
            {
                if (!liquid.GetPreparedCookingIngredient().IsNullOrEmpty())
                {
                    return true;
                }
            }
            return false;
        }

        public string GetPreparedCookingIngredient()
        {
            if (Volume == 0)
            {
                return "";
            }
            StringBuilder SB = Event.NewStringBuilder();
            foreach (BaseLiquid liquid in GetComponentLiquids())
            {
                string ingredientType = liquid.GetPreparedCookingIngredient();
                if (!ingredientType.IsNullOrEmpty())
                {
                    if (SB.Length != 0)
                    {
                        SB.Append(',');
                    }
                    SB.Append(ingredientType);
                }
            }
            return SB.ToString();
        }

        public List<BaseLiquid> GetComponentLiquids()
        {
            List<BaseLiquid> ret = new List<BaseLiquid>(2);
            BaseLiquid liquid1 = GetPrimaryLiquid();
            if (liquid1 != null)
            {
                ret.Add(liquid1);
            }
            BaseLiquid liquid2 = GetSecondaryLiquid();
            if (liquid2 != null)
            {
                ret.Add(liquid2);
            }
            return ret;
        }

        public bool HasLiquid(string ID)
        {
            return ComponentLiquids?.Any(k => k.Key == ID && k.Value > 0) ?? false;
        }

        public bool HasPrimaryOrSecondaryLiquid(string ID)
        {
            if (GetPrimaryLiquidID() == ID)
            {
                return true;
            }
            if (GetSecondaryLiquidID() == ID)
            {
                return true;
            }
            return false;
        }

        public BaseLiquid GetPrimaryLiquid()
        {
            if (Volume <= 0)
            {
                return null;
            }
            if (ComponentLiquids == null)
            {
                return null;
            }
            if (ComponentLiquids.Count <= 0)
            {
                return null;
            }
            if (Primary == null)
            {
                RecalculatePrimary();
                if (Primary == null)
                {
                    return null;
                }
            }
            BaseLiquid result;
            if (!Liquids.TryGetValue(Primary, out result))
            {
                return null;
            }
            return result;
        }

        public string GetPrimaryLiquidID()
        {
            return GetPrimaryLiquid()?.ID;
        }

        public BaseLiquid RequirePrimaryLiquid()
        {
            if (Primary == null)
            {
                if (Volume <= 0)
                {
                    throw new Exception("no liquid");
                }
                if (ComponentLiquids == null)
                {
                    throw new Exception("no component liquid list");
                }
                if (ComponentLiquids.Count <= 0)
                {
                    throw new Exception("empty component liquid");
                }
                RecalculatePrimary();
                if (Primary == null)
                {
                    throw new Exception("primary liquid cannot be determined");
                }
            }
            BaseLiquid result;
            if (!Liquids.TryGetValue(Primary, out result))
            {
                throw new Exception("primary liquid \"" + Primary + "\" unknown");
            }
            return result;
        }

        public BaseLiquid GetSecondaryLiquid()
        {
            if (Volume <= 0)
            {
                return null;
            }
            if (ComponentLiquids == null)
            {
                return null;
            }
            if (ComponentLiquids.Count <= 1)
            {
                return null;
            }
            if (Secondary == null)
            {
                RecalculatePrimary();
                if (Secondary == null)
                {
                    return null;
                }
            }
            BaseLiquid result;
            if (!Liquids.TryGetValue(Secondary, out result))
            {
                return null;
            }
            return result;
        }

        public string GetSecondaryLiquidID()
        {
            return GetSecondaryLiquid()?.ID;
        }

        public BaseLiquid RequireSecondaryLiquid()
        {
            if (Secondary == null)
            {
                if (Volume <= 0)
                {
                    throw new Exception("no liquid");
                }
                if (ComponentLiquids == null)
                {
                    throw new Exception("no component liquid list");
                }
                if (ComponentLiquids.Count <= 0)
                {
                    throw new Exception("empty component liquid");
                }
                RecalculatePrimary();
                if (Secondary == null)
                {
                    throw new Exception("secondary liquid cannot be determined");
                }
            }
            BaseLiquid result;
            if (!Liquids.TryGetValue(Secondary, out result))
            {
                throw new Exception("secondary liquid \"" + Secondary + "\" unknown");
            }
            return result;
        }

        /*public override void LoadData(SerializationReader Reader)
        {
            base.LoadData(Reader);
            ComponentLiquids = Reader.ReadDictionary<string, int>();
        }

        public override void SaveData(SerializationWriter Writer)
        {
            base.SaveData(Writer);
            Writer.Write<string, int>(ComponentLiquids);
        }*/

        public override IPart DeepCopy(GameObject Parent, Func<GameObject, GameObject> MapInv)
        {
            LiquidVolume LiquidVolume = base.DeepCopy(Parent, MapInv) as LiquidVolume;
            LiquidVolume.ComponentLiquids = new Dictionary<string, int>(ComponentLiquids.Count);
            foreach (var pair in ComponentLiquids)
            {
                LiquidVolume.ComponentLiquids.Add(pair.Key, pair.Value);
            }
            return LiquidVolume;
        }

        public string SmearedName
        {
            get
            {
                if (_SmearedName == null)
                {
                    FindSmear();
                }
                return _SmearedName;
            }
        }

        public string SmearedColor
        {
            get
            {
                if (_SmearedColor == null && _SmearedName == null)
                {
                    FindSmear();
                }
                return _SmearedColor;
            }
        }

        private void FindSmear()
        {
            if (ComponentLiquids.Count == 1)
            {
                string Name = null;
                if (Primary == null)
                {
                    foreach (string Liquid in GetTertiaries())
                    {
                        BaseLiquid L = GetLiquid(Liquid);
                        Name = L.GetSmearedName(this);
                        if (!Name.IsNullOrEmpty())
                        {
                            _SmearedColor = L.GetColor();
                            break;
                        }
                    }
                }
                else
                {
                    BaseLiquid PL = RequirePrimaryLiquid();
                    Name = PL.GetSmearedName(this);
                    _SmearedColor = PL.GetColor();
                }
                _SmearedName = Name ?? "liquid-covered";
            }
            else
            {
                StringBuilder SB = Event.NewStringBuilder();
                if (Secondary != null)
                {
                    BaseLiquid SL = RequireSecondaryLiquid();
                    string Adj = SL.GetSmearedAdjective(this);
                    if (!Adj.IsNullOrEmpty())
                    {
                        SB.Compound(Adj);
                        _SmearedColor = SL.GetColor();
                    }
                }
                List<string> Tertiaries = GetTertiaries();
                if (Tertiaries != null)
                {
                    foreach (string Liquid in Tertiaries)
                    {
                        BaseLiquid L = GetLiquid(Liquid);
                        string Adj = L.GetSmearedAdjective(this);
                        if (!Adj.IsNullOrEmpty())
                        {
                            SB.Compound(Adj);
                            if (_SmearedColor == null)
                            {
                                _SmearedColor = L.GetColor();
                            }
                        }
                    }
                }
                if (Primary != null)
                {
                    BaseLiquid PL = RequirePrimaryLiquid();
                    string Name = RequirePrimaryLiquid().GetSmearedName(this);
                    if (!Name.IsNullOrEmpty())
                    {
                        SB.Compound(Name);
                        _SmearedColor = PL.GetColor() ?? _SmearedColor;
                    }
                }
                _SmearedName = SB.Length > 0 ? SB.ToString() : "liquid-covered";
            }
        }

        public void ProcessSmear(GetDisplayNameEvent E)
        {
            if (E.Visible && !E.Reference)
            {
                E.AddAdjective(SmearedName, DescriptionBuilder.ORDER_ADJUST_EARLY + DescriptionBuilder.ORDER_ADJUST_SLIGHTLY_EARLY);
                if (!_SmearedColor.IsNullOrEmpty())
                {
                    E.AddColor(_SmearedColor, DescriptionBuilder.PRIORITY_MEDIUM + DescriptionBuilder.PRIORITY_ADJUST_MEDIUM);
                }
            }
        }

        public string StainedName
        {
            get
            {
                if (_StainedName == null)
                {
                    FindStain();
                }
                return _StainedName;
            }
        }

        public string StainedColor
        {
            get
            {
                if (_StainedColor == null && _StainedName == null)
                {
                    FindStain();
                }
                return _StainedColor;
            }
        }

        private void FindStain()
        {
            if (ComponentLiquids.Count == 1)
            {
                string Name = null;
                if (Primary == null)
                {
                    foreach (string Liquid in GetTertiaries())
                    {
                        BaseLiquid L = GetLiquid(Liquid);
                        Name = L.GetStainedName(this);
                        if (!Name.IsNullOrEmpty())
                        {
                            _StainedColor = L.GetColor();
                            break;
                        }
                    }
                }
                else
                {
                    BaseLiquid L = RequirePrimaryLiquid();
                    Name = L.GetStainedName(this);
                    _StainedColor = L.GetColor();
                }
                if (Name != null)
                {
                    _StainedName = Name + "-stained";
                }
            }
            else
            {
                StringBuilder SB = Event.NewStringBuilder();
                if (Primary != null)
                {
                    BaseLiquid PL = RequirePrimaryLiquid();
                    BaseLiquid SL = (Secondary != null) ? RequireSecondaryLiquid() : null;
                    _StainedColor = PL.GetColor() ?? (SL != null ? SL.GetColor() : null);
                    string PrimaryName = PL.GetStainedName(this);
                    if (!PrimaryName.IsNullOrEmpty())
                    {
                        SB.Append(PrimaryName);
                    }
                    if (SL != null)
                    {
                        string SecondaryName = SL.GetStainedName(this);
                        if (!SecondaryName.IsNullOrEmpty())
                        {
                            if (!PrimaryName.IsNullOrEmpty())
                            {
                                SB.Append("-and-");
                            }
                            SB.Append(SecondaryName);
                        }
                    }
                }
                if (SB.Length <= 0)
                {
                    foreach (string Liquid in GetTertiaries())
                    {
                        BaseLiquid L = GetLiquid(Liquid);
                        string Name = L.GetStainedName(this);
                        if (Name != null)
                        {
                            _StainedColor = L.GetColor();
                            SB.Append(Name);
                            break;
                        }
                    }
                }
                if (SB.Length > 0)
                {
                    SB.Append("-stained");
                    _StainedName = SB.ToString();
                }
            }
            if (_StainedName == null)
            {
                _StainedName = "stained";
            }
        }

        public void ProcessStain(GetDisplayNameEvent E)
        {
            if (E.Visible && !E.Reference)
            {
                E.AddAdjective(StainedName, DescriptionBuilder.ORDER_ADJUST_EARLY);
                if (!_StainedColor.IsNullOrEmpty())
                {
                    E.AddColor(_StainedColor, DescriptionBuilder.PRIORITY_MEDIUM);
                }
            }
        }

        public int Proportion(string Liquid)
        {
            int result;
            if (ComponentLiquids.TryGetValue(Liquid, out result))
            {
                return result;
            }
            return 0;
        }

        public int Proportion(string Liquid1, string Liquid2)
        {
            return Proportion(Liquid1) + Proportion(Liquid2);
        }

        public int Proportion(string Liquid1, string Liquid2, string Liquid3)
        {
            return Proportion(Liquid1) + Proportion(Liquid2) + Proportion(Liquid3);
        }

        public int Proportion(string Liquid1, string Liquid2, string Liquid3, string Liquid4)
        {
            return Proportion(Liquid1) + Proportion(Liquid2) + Proportion(Liquid3) + Proportion(Liquid4);
        }

        public int Proportion(string Liquid1, string Liquid2, string Liquid3, string Liquid4, string Liquid5)
        {
            return Proportion(Liquid1) + Proportion(Liquid2) + Proportion(Liquid3) + Proportion(Liquid4) + Proportion(Liquid5);
        }

        public int Amount(string Liquid)
        {
            return Volume * Proportion(Liquid) / 1000;
        }

        public int Amount(string Liquid1, string Liquid2)
        {
            return Volume * Proportion(Liquid1, Liquid2) / 1000;
        }

        public int Amount(string Liquid1, string Liquid2, string Liquid3)
        {
            return Volume * Proportion(Liquid1, Liquid2, Liquid3) / 1000;
        }

        public int Amount(string Liquid1, string Liquid2, string Liquid3, string Liquid4)
        {
            return Volume * Proportion(Liquid1, Liquid2, Liquid3, Liquid4) / 1000;
        }

        public int Amount(string Liquid1, string Liquid2, string Liquid3, string Liquid4, string Liquid5)
        {
            return Volume * Proportion(Liquid1, Liquid2, Liquid3, Liquid4, Liquid5) / 1000;
        }

        public int UpperAmount(string Liquid)
        {
            return (int) Math.Ceiling(Volume * Proportion(Liquid) / 1000.0);
        }

        public int UpperAmount(string Liquid1, string Liquid2)
        {
            return (int) Math.Ceiling(Volume * Proportion(Liquid1, Liquid2) / 1000.0);
        }

        public int UpperAmount(string Liquid1, string Liquid2, string Liquid3)
        {
            return (int) Math.Ceiling(Volume * Proportion(Liquid1, Liquid2, Liquid3) / 1000.0);
        }

        public int UpperAmount(string Liquid1, string Liquid2, string Liquid3, string Liquid4)
        {
            return (int) Math.Ceiling(Volume * Proportion(Liquid1, Liquid2, Liquid3, Liquid4) / 1000.0);
        }

        public int UpperAmount(string Liquid1, string Liquid2, string Liquid3, string Liquid4, string Liquid5)
        {
            return (int) Math.Ceiling(Volume * Proportion(Liquid1, Liquid2, Liquid3, Liquid4, Liquid5) / 1000.0);
        }

        public int MilliAmount(string Liquid)
        {
            return Volume * Proportion(Liquid);
        }

        public int MilliAmount(string Liquid1, string Liquid2)
        {
            return Volume * Proportion(Liquid1, Liquid2);
        }

        public int MilliAmount(string Liquid1, string Liquid2, string Liquid3)
        {
            return Volume * Proportion(Liquid1, Liquid2, Liquid3);
        }

        public int MilliAmount(string Liquid1, string Liquid2, string Liquid3, string Liquid4)
        {
            return Volume * Proportion(Liquid1, Liquid2, Liquid3, Liquid4);
        }

        public int MilliAmount(string Liquid1, string Liquid2, string Liquid3, string Liquid4, string Liquid5)
        {
            return Volume * Proportion(Liquid1, Liquid2, Liquid3, Liquid4, Liquid5);
        }

        public bool IsMixed()
        {
            return ComponentLiquids.Count > 1;
        }

        public bool IsPure()
        {
            return ComponentLiquids.Count <= 1;
        }

        private static List<KeyValuePair<string, int>> SortedComponents = new List<KeyValuePair<string, int>>(4);
        public bool RecalculatePrimary()
        {
            _SmearedName = null;
            _SmearedColor = null;
            _StainedName = null;
            _StainedColor = null;
            string OldPrimary = Primary;
            string OldSecondary = Secondary;
            if (ComponentLiquids.Count == 0)
            {
                Primary = null;
                Secondary = null;
            }
            else if (ComponentLiquids.Count == 1)
            {
                if (Primary != null && ComponentLiquids.ContainsKey(Primary))
                {
                    return false;
                }

                Primary = ComponentLiquids.First().Key;
                Secondary = null;
            }
            else
            {
                SortedComponents.Clear();
                foreach (var pair in ComponentLiquids)
                {
                    var i = SortedComponents.BinarySearch(pair, ComponentSorter.Instance);
                    SortedComponents.Insert(i < 0 ? ~i : i, pair);
                }

                Primary = SortedComponents[0].Key;
                Secondary = SortedComponents[1].Key;
                if (Primary != LiquidBlood.ID && ComponentLiquids.ContainsKey(LiquidBlood.ID))
                {
                    Secondary = LiquidBlood.ID;
                }
                else if (Primary == LiquidWarmStatic.ID && Secondary == LiquidWater.ID)
                {
                    Secondary = ComponentLiquids.Count == 2 ? null : SortedComponents[2].Key;
                }
            }

            if (Primary != OldPrimary || Secondary != OldSecondary)
            {
                if (IsOpenVolume())
                {
                    BaseRender();
                }
                return true;
            }
            return false;
        }

        public static void Init()
        {
            _Liquids = new StringMap<BaseLiquid>();

            foreach (var liquidType in ModManager.GetTypesWithAttribute(typeof(IsLiquid)))
            {
                try
                {
                    // Logger.gameLog.Info("initializing liquid: " + liquidType.Name );
                    var newLiquid = Activator.CreateInstance(liquidType) as BaseLiquid;
                    if (newLiquid == null)
                    {
                        Logger.gameLog.Error("couldn't instantiate " + liquidType + " or it is not derived from BaseLiquid.");
                    }
                    else
                    {
                        _Liquids[newLiquid.ID] = newLiquid;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception("Initializing liquid type " + liquidType.Name, ex);
                }
            }
        }

        public static List<string> GetLiquidColors(string liquid)
        {
            return Liquids[liquid].GetColors();
        }

        public string InitialLiquid
        {
            //
            // "water-500,salt-500"
            // "water-1000"
            // "0-500,1-500"
            // "water-500,salt-500;water-1000" (randomly salty water or pure water)
            //
            set
            {
                if (value == "")
                {
                    return;
                }
                ComponentLiquids.Clear();
                string UseSpec = value.Contains(";") ? value.Split(';').GetRandomElement() : value;
                if (UseSpec.Contains(","))
                {
                    foreach (string LiquidSpec in UseSpec.Split(','))
                    {
                        try
                        {
                            ProcessInitialLiquid(LiquidSpec);
                        }
                        catch (Exception ex)
                        {
                            MetricsManager.LogError("InitialLiquid " + UseSpec, ex);
                        }
                    }
                }
                else
                {
                    ProcessInitialLiquid(UseSpec);
                }
                NormalizeProportions();
                RecalculatePrimary();
                if (ParentObject?.CurrentCell != null)
                {
                    CheckImage();
                }
            }
        }


        private void ProcessInitialLiquid(string Spec)
        {
            try
            {
                if (Spec.Contains('-'))
                {
                    string[] Parts = Spec.Split('-');
                    string LiquidID = Parts[0];
                    int Proportion = Convert.ToInt32(Parts[1]);
                    ComponentLiquids.Add(LiquidID, Proportion);
                }
                else
                {
                    string LiquidID = Liquids[Spec].ID;
                    ComponentLiquids.Add(LiquidID, 1000);
                }
            }
            catch
            {
                MetricsManager.LogError("invalid initial liquid specification " + (Spec ?? "NULL"));
            }
        }
        public bool IsEmpty()
        {
            return (ComponentLiquids.Count == 0 || Volume == 0);
        }

        public int GetNavigationWeight(GameObject Object, bool Smart, bool Slimewalking, bool FilthAffinity, ref bool Uncacheable)
        {
            int result = 0;
            foreach (string liquidID in ComponentLiquids.Keys)
            {
                BaseLiquid liquid = Liquids[liquidID];
                int weight = liquid.GetNavigationWeight(this, Object, Smart, Slimewalking, FilthAffinity, ref Uncacheable);
                if (weight < 2 && liquid.InterruptAutowalk)
                {
                    weight = 2;
                }
                if (weight > result)
                {
                    result = weight;
                }
            }
            return result;
        }

        private void TrackAsLiquid(string Liquid)
        {
            if (Liquid.IndexOf(',') == -1)
            {
                string LiquidID = Liquid;
                if (ComponentLiquids.Count > 1 || !ComponentLiquids.ContainsKey(LiquidID))
                {
                    ComponentLiquids.Clear();
                    ComponentLiquids.Add(LiquidID, 1000);
                }
                else
                {
                    ComponentLiquids[LiquidID] = 1000;
                }
            }
            else
            {
                ComponentLiquids.Clear();
                int Total = 0;
                foreach (string Part in Liquid.CachedCommaExpansion())
                {
                    string[] Specs = Part.Split('-');
                    if (Specs.Length != 2)
                    {
                        MetricsManager.LogWarning("Invalid liquid specification: " + Part);
                        return;
                    }
                    string Name = Specs[0];
                    string ProportionSpec = Specs[1];
                    int Proportion;
                    try
                    {
                        Proportion = Convert.ToInt32(ProportionSpec);
                    }
                    catch
                    {
                        return;
                    }
                    if (!Liquids.ContainsKey(Name))
                    {
                        MetricsManager.LogWarning("Unknown liquid type: " + Name);
                        return;
                    }
                    string LiquidID = Liquids[Name].ID;
                    ComponentLiquids.Add(LiquidID, Proportion);
                    Total += Proportion;
                }
                NormalizeProportions();
            }
        }

        public bool IsPureLiquid(bool AllowEmpty = false)
        {
            if (Volume == 0 || ComponentLiquids == null)
            {
                return AllowEmpty;
            }
            return ComponentLiquids.Count == 1;
        }

        public bool IsPureLiquid(string LiquidType, bool AllowEmpty = false)
        {
            if (LiquidType.IsNullOrEmpty())
            {
                return false;
            }
            if (Volume == 0 || ComponentLiquids == null)
            {
                return AllowEmpty;
            }
            if (LiquidType.IndexOf(',') == -1)
            {
                if (ComponentLiquids.Count != 1)
                {
                    return false;
                }
                if (LiquidType.EndsWith("-1000"))
                {
                    LiquidType = LiquidType.Substring(0, LiquidType.LastIndexOf('-'));
                }
                if (!ComponentLiquids.ContainsKey(LiquidType))
                {
                    return false;
                }
                return true;
            }
            else
            {
                if (ComponentLiquids.Count < 2)
                {
                    return false;
                }
                string[] Parts = LiquidType.Split(',');
                if (ComponentLiquids.Count != Parts.Length)
                {
                    return false;
                }
                foreach (string Part in Parts)
                {
                    string[] Specs = Part.Split('-');
                    if (Specs.Length != 2)
                    {
                        MetricsManager.LogWarning("Invalid liquid specification: " + Part);
                        return false;
                    }
                    string Name = Specs[0];
                    string ProportionSpec = Specs[1];
                    int Proportion;
                    try
                    {
                        Proportion = Convert.ToInt32(ProportionSpec);
                    }
                    catch
                    {
                        return false;
                    }
                    if (!LiquidVolume.isValidLiquid(Name))
                    {
                        MetricsManager.LogWarning("Unknown liquid type: " + Name);
                        return false;
                    }
                    int HaveProportion;
                    if (!ComponentLiquids.TryGetValue(Name, out HaveProportion))
                    {
                        return false;
                    }
                    if (HaveProportion != Proportion)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public bool ContainsLiquid(string LiquidID)
        {
            return ComponentLiquids.ContainsKey(LiquidID);
        }

        public bool ContainsSignificantLiquid(string LiquidID)
        {
            if (Volume < 1)
            {
                return false;
            }
            if (!ComponentLiquids.ContainsKey(LiquidID))
            {
                return false;
            }
            if (ComponentLiquids.Count == 1)
            {
                return true;
            }
            int Proportion = ComponentLiquids[LiquidID];
            return (Volume * Proportion / 1000) >= 1;
        }

        public bool IsWaste()
        {
            return ContainsSignificantLiquid(LiquidOoze.ID) || ContainsSignificantLiquid(LiquidSludge.ID) || ContainsSignificantLiquid(LiquidGoo.ID);
        }

        public bool IsWater(bool AllowEmpty = false)
        {
            return ContainsSignificantLiquid(LiquidWater.ID);
        }

        public bool IsFreshWater(bool AllowEmpty = false)
        {
            return IsPureLiquid(LiquidWater.ID, AllowEmpty);
        }

        [NonSerialized] private static List<string> CombinedLiquidTypes = new List<string>();
        [NonSerialized] private static List<string> IncomingLiquidTypes = new List<string>();

        public bool MixWith(
            LiquidVolume Liquid,
            ref bool RequestInterfaceExit,
            GameObject PouredFrom = null,
            GameObject PouredBy = null,
            int? Amount = null,
            bool UseTempSplit = false
        )
        {
            int TargetVolume = Volume;
            int MixingVolume = Amount ?? Liquid.Volume;
            if (ParentObject != null && ParentObject.HasRegisteredEvent("LiquidMixing"))
            {
                Event E = Event.New("LiquidMixing");
                E.SetParameter("TargetVolume", this);
                E.SetParameter("MixingVolume", Liquid);
                E.SetParameter("Amount", MixingVolume);
                E.SetParameter("PouredFrom", PouredFrom);
                E.SetParameter("PouredTo", ParentObject);
                E.SetParameter("PouredBy", PouredBy);
                if (!ParentObject.FireEvent(E))
                {
                    return false;
                }
            }
            IncomingLiquidTypes.Clear();
            IncomingLiquidTypes.AddRange(Liquid.ComponentLiquids.Keys);
            CombinedLiquidTypes.Clear();
            foreach (string LiquidID in Liquid.ComponentLiquids.Keys)
            {
                bool result = Liquids[LiquidID].MixingWith(
                    Liquid: Liquid,
                    NewLiquid: this,
                    Amount: MixingVolume,
                    PouredFrom: PouredFrom,
                    PouredTo: ParentObject,
                    PouredBy: PouredBy,
                    ExitInterface: ref RequestInterfaceExit
                );
                if (!result)
                {
                    return false;
                }
                if (!CombinedLiquidTypes.Contains(LiquidID))
                {
                    CombinedLiquidTypes.Add(LiquidID);
                }
            }
            foreach (string LiquidID in ComponentLiquids.Keys)
            {
                bool result = Liquids[LiquidID].MixingWith(
                    Liquid: Liquid,
                    NewLiquid: this,
                    Amount: MixingVolume,
                    PouredFrom: PouredFrom,
                    PouredTo: ParentObject,
                    PouredBy: PouredBy,
                    ExitInterface: ref RequestInterfaceExit
                );
                if (!result)
                {
                    return false;
                }
                if (!CombinedLiquidTypes.Contains(LiquidID))
                {
                    CombinedLiquidTypes.Add(LiquidID);
                }
            }
            if (Amount != null)
            {
                Liquid = UseTempSplit ? Liquid.TempSplit((int) Amount) : Liquid.Split((int) Amount);
            }
            int highNewPermillage = int.MinValue;
            string highNewPermillageLiquidID = null;
            foreach (string LiquidID in CombinedLiquidTypes)
            {
                int TargetProportion = ComponentLiquids.ContainsKey(LiquidID) ? ComponentLiquids[LiquidID] : 0;
                int MixingProportion = Liquid.ComponentLiquids.ContainsKey(LiquidID) ? Liquid.ComponentLiquids[LiquidID] : 0;
                int newPermillage = (int) Math.Floor((double) ((TargetProportion * TargetVolume) + (MixingProportion * MixingVolume)) / (double) (TargetVolume + MixingVolume));
                if (newPermillage > highNewPermillage || (newPermillage == highNewPermillage && (highNewPermillageLiquidID == null || highNewPermillageLiquidID.CompareTo(LiquidID) < 0)))
                {
                    highNewPermillage = newPermillage;
                    highNewPermillageLiquidID = LiquidID;
                }
                if (newPermillage < 1 && MixingVolume > 0 && MixingProportion > 0)
                {
                    newPermillage = 1;
                }
                if (newPermillage > 0)
                {
                    ComponentLiquids[LiquidID] = newPermillage;
                }
                else
                {
                    ComponentLiquids.Remove(LiquidID);
                }
            }
            Volume += MixingVolume;
            if (MaxVolume >= 0 && Volume > MaxVolume)
            {
                Volume = MaxVolume;
            }
            if (Volume > 0 && ComponentLiquids.Count == 0 && highNewPermillageLiquidID != null)
            {
                ComponentLiquids.Add(highNewPermillageLiquidID, 1000);
            }
            if (MixingVolume > 0 && ParentObject != null && !IsOpenVolume())
            {
                foreach (string l in IncomingLiquidTypes)
                {
                    Liquids[l].FillingContainer(ParentObject, this);
                }
            }
            foreach (string l in new List<string>(CombinedLiquidTypes))
            {
                Liquids[l].MixedWith(
                    Liquid: Liquid,
                    NewLiquid: this,
                    Amount: MixingVolume,
                    PouredFrom: PouredFrom,
                    PouredTo: ParentObject,
                    PouredBy: PouredBy,
                    ExitInterface: ref RequestInterfaceExit
                );
            }
            Update();
            if (ParentObject != null)
            {
                LiquidMixedEvent.Send(this);
            }
            return true;
        }

        public bool MixWith(
            LiquidVolume Liquid,
            GameObject PouredFrom = null,
            GameObject PouredBy = null,
            int? Amount = null,
            bool UseTempSplit = false
        )
        {
            bool RequestInterfaceExit = false;
            return MixWith(
                Liquid: Liquid,
                RequestInterfaceExit: ref RequestInterfaceExit,
                PouredFrom: PouredFrom,
                PouredBy: PouredBy,
                Amount: Amount,
                UseTempSplit: UseTempSplit
            );
        }

        public void NormalizeProportions()
        {
            if (ComponentLiquids.Count == 0)
            {
                return;
            }
            int TotalProportion = 0;
            foreach (int Proportion in ComponentLiquids.Values)
            {
                TotalProportion += Proportion;
            }
            if (TotalProportion == 1000)
            {
                return;
            }
            if (ComponentLiquids.Count > 1)
            {
                int Diff = 1000 - TotalProportion;
                if (Diff > 1 || Diff < -1)
                {
                    Dictionary<string, int> Proportional = new Dictionary<string, int>(ComponentLiquids.Count);
                    foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                    {
                        int Value = Diff * KV.Value / TotalProportion;
                        if (Value != 0)
                        {
                            Proportional[KV.Key] = Value;
                        }
                    }
                    foreach (KeyValuePair<string, int> KV in Proportional)
                    {
                        ComponentLiquids[KV.Key] += KV.Value;
                        Diff -= KV.Value;
                    }
                }
                if (Diff == 1 || Diff == -1)
                {
                    int HighestProportion = int.MinValue;
                    string HighestLiquid = null;
                    foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                    {
                        if (KV.Value > HighestProportion)
                        {
                            HighestLiquid = KV.Key;
                            HighestProportion = KV.Value;
                        }
                    }
                    if (HighestLiquid != null)
                    {
                        if (Diff > 0)
                        {
                            ComponentLiquids[HighestLiquid]++;
                            Diff--;
                        }
                        else
                        {
                            ComponentLiquids[HighestLiquid]--;
                            Diff++;
                        }
                    }
                }
                else
                if (Diff != 0)
                {
                    List<KeyValuePair<string, int>> Descending = ComponentLiquids.ToList();
                    Descending.Sort((a, b) => b.Value.CompareTo(a.Value));
                    while (Diff != 0)
                    {
                        foreach (KeyValuePair<string, int> KV in Descending)
                        {
                            if (Diff > 0)
                            {
                                ComponentLiquids[KV.Key]++;
                                Diff--;
                            }
                            else
                            {
                                ComponentLiquids[KV.Key]--;
                                Diff++;
                            }
                            if (Diff == 0)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                string ID = null;
                foreach (string LiquidID in ComponentLiquids.Keys)
                {
                    ID = LiquidID;
                    break;
                }
                if (ID != null)
                {
                    ComponentLiquids[ID] = 1000;
                }
            }
            FlushWeightCaches();
        }

        public string GetLiquidDesignation( bool hyphenateSoloLiquids = false )
        {
            if (ComponentLiquids.Count == 0)
            {
                return null;
            }
            if (ComponentLiquids.Count == 1)
            {
                foreach (string LiquidID in ComponentLiquids.Keys)
                {
                    if(hyphenateSoloLiquids)
                    {
                        return $"{LiquidID}-1000";
                    }
                    else
                    {
                        return LiquidID;
                    }
                }
            }
            string[] Parts = new string[ComponentLiquids.Count];
            int ix = 0;
            foreach (KeyValuePair<string, int> Item in ComponentLiquids)
            {
                Parts[ix++] = Item.Key + '-' + Item.Value;
            }
            return string.Join(",", Parts);
        }

        public string GetLiquidDebugDesignation()
        {
            string what = GetLiquidDesignation() ?? "nothing";
            return Volume + "/" + MaxVolume + "(" + what + ")";
        }

        public void SetComponent(string b, int value)
        {
            if (value == 0)
            {
                if (ComponentLiquids.ContainsKey(b))
                {
                    int remaining = ComponentLiquids[b];
                    ComponentLiquids.Remove(b);

                    foreach (var (by, _) in Liquids)
                    {
                        if (by != b && ComponentLiquids.ContainsKey(by))
                        {
                            ComponentLiquids[by] += (int) (remaining * ((float) ComponentLiquids[by] / (float) 1000));
                        }
                    }
                }
            }
            else
            {
                int remaining;
                if (ComponentLiquids.ContainsKey(b))
                {
                    remaining = 1000 - (value - ComponentLiquids[b]);
                }
                else
                {
                    remaining = 1000 - value;
                }
                ComponentLiquids[b] = value;

                foreach (var (by, _) in Liquids)
                {
                    if (by != b && ComponentLiquids.ContainsKey(by))
                    {
                        ComponentLiquids[by] = (int) (remaining * ((float) ComponentLiquids[by] / (float) 1000));
                    }
                }
            }
            Update();
        }

        public void Update()
        {
            if (Volume <= 0)
            {
                Empty();
            }
            else
            {
                NormalizeProportions();
                RecalculatePrimary();
                RecalculateProperties();
            }
        }

        public void RecalculateProperties()
        {
            if (ParentObject != null)
            {
                SyncPhysicalProperties();
                CheckImage();
                FlushWeightCaches();
            }
        }

        public void SyncPhysicalProperties()
        {
            if (ParentObject?.Physics != null && IsOpenVolume())
            {
                int conductivity;
                GetLiquidPhysicalProperties(
                    out ParentObject.Physics.FlameTemperature,
                    out ParentObject.Physics.VaporTemperature,
                    out ParentObject.Physics.FreezeTemperature,
                    out ParentObject.Physics.BrittleTemperature,
                    out conductivity
                );
                ParentObject.BaseElectricalConductivity = conductivity;
            }
        }

        private static LiquidVolume tempVolume = new LiquidVolume();
        public LiquidVolume TempSplit(int SplitVolume)
        {
            LiquidVolume TempVolume = tempVolume;
            // If we're splitting into a zero-volume liquid, add a dram so we retain the component liquid information (since LV wipes it on zero-volume liquids)
            TempVolume.Volume = (SplitVolume == 0) ? 1 : SplitVolume;
            TempVolume.ComponentLiquids.Clear();
            foreach (KeyValuePair<string, int> KV in ComponentLiquids)
            {
                TempVolume.ComponentLiquids.Add(KV.Key, KV.Value);
            }
            Volume -= SplitVolume;
            if (Volume <= 0)
            {
                Empty();
            }
            else
            {
                FlushWeightCaches();
            }
            return TempVolume;
        }

        public LiquidVolume Split(int SplitVolume)
        {
            LiquidVolume NewVolume = new LiquidVolume();
            // If we're splitting into a zero-volume liquid, add a dram so we retain the component liquid information (since LV wipes it on zero-volume liquids)
            NewVolume.Volume = (SplitVolume == 0) ? 1 : SplitVolume;
            foreach (KeyValuePair<string, int> KV in ComponentLiquids)
            {
                NewVolume.ComponentLiquids.Add(KV.Key, KV.Value);
            }
            Volume -= SplitVolume;
            if (Volume <= 0)
            {
                Empty();
            }
            else
            {
                FlushWeightCaches();
            }
            return NewVolume;
        }

        public bool LiquidSameAs(LiquidVolume V)
        {
            if (V == null)
            {
                return false;
            }
            if (V.ComponentLiquids.Count != ComponentLiquids.Count)
            {
                return false;
            }
            foreach (KeyValuePair<string, int> KV in V.ComponentLiquids)
            {
                int Proportion;
                if (!ComponentLiquids.TryGetValue(KV.Key, out Proportion))
                {
                    return false;
                }
                if (Proportion != KV.Value)
                {
                    return false;
                }
            }
            return true;
        }

        public override bool SameAs(IPart p)
        {
            return false;
        }

        public override bool WantEvent(int ID, int cascade)
        {
            return
                base.WantEvent(ID, cascade)
                || ID == AllowLiquidCollectionEvent.ID
                || ID == AnyAutoCollectDramsEvent.ID
                || ID == AutoexploreObjectEvent.ID
                || ID == CheckAnythingToCleanWithEvent.ID
                || ID == CheckAnythingToCleanWithNearbyEvent.ID
                || ID == EffectAppliedEvent.ID
                || ID == EffectRemovedEvent.ID
                || ID == EnteredCellEvent.ID
                || ID == BeforeRenderEvent.ID
                || ID == CanSmartUseEvent.ID
                || ID == CommandSmartUseEvent.ID
                || ID == FellDownEvent.ID
                || ID == FrozeEvent.ID
                || ID == GetAutoCollectDramsEvent.ID
                || ID == GetCleaningItemsEvent.ID
                || ID == GetCleaningItemsNearbyEvent.ID
                || ID == GetDebugInternalsEvent.ID
                || ID == GetDisplayNameEvent.ID
                || ID == GetExtrinsicValueEvent.ID
                || ID == GetExtrinsicWeightEvent.ID
                || ID == GetFreeDramsEvent.ID
                || ID == GetGameObjectSortEvent.ID
                || ID == GetInventoryActionsAlwaysEvent.ID
                || ID == GetMatterPhaseEvent.ID
                || ID == GetNavigationWeightEvent.ID
                || ID == GetShortDescriptionEvent.ID
                || ID == GetSlottedInventoryActionsEvent.ID
                || ID == GetSpringinessEvent.ID
                || ID == GetStorableDramsEvent.ID
                || ID == GiveDramsEvent.ID
                || ID == GravitationEvent.ID
                || ID == InterruptAutowalkEvent.ID
                || ID == InventoryActionEvent.ID
                || ID == ObjectCreatedEvent.ID
                || ID == ObjectEnteredCellEvent.ID
                || ID == ObjectGoingProneEvent.ID
                || ID == ObjectStoppedFlyingEvent.ID
                || ID == OnDestroyObjectEvent.ID
                || ID == PollForHealingLocationEvent.ID
                || ID == RadiatesHeatEvent.ID
                || ID == StripContentsEvent.ID
                || ID == ThawedEvent.ID
                || ID == UseDramsEvent.ID
                || ID == VaporizedEvent.ID
            ;
        }

        public override bool HandleEvent(AutoexploreObjectEvent E)
        {
            if (
                E.Command == null
                && !IsMixed()
                && GetPrimaryLiquidID() is string liquid
                && (
                    !Options.AutogetNoDroppedLiquid
                    || ParentObject.GetIntProperty("DroppedByPlayer") <= 0
                )
                && !ParentObject.IsOwned()
                && !EffectivelySealed()
                && !ParentObject.IsInStasis()
                && !ParentObject.IsTemporary
                && E.Actor.AnyAutoCollectDrams(liquid)
            )
            {
                E.Command = "CollectLiquid";
                E.AllowRetry = true;
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetSpringinessEvent E)
        {
            if (IsOpenVolume() && !ParentObject.IsFrozen())
            {
                E.LinearIncrease += ParentObject.GetKineticResistance() * 95 / 100;
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(RadiatesHeatEvent E)
        {
            if (IsOpenVolume() && GetLiquidTemperature() > 25)
            {
                return false; // positive case
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetMatterPhaseEvent E)
        {
            if (IsOpenVolume())
            {
                E.MinMatterPhase(MatterPhase.LIQUID);
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetNavigationWeightEvent E)
        {
            if (!E.Flying && IsOpenVolume() && E.PhaseMatches(ParentObject) && CanInteractWithAnything(E.Cell))
            {
                int weight = GetNavigationWeight(E.Actor, E.Smart, E.Slimewalking, E.FilthAffinity, ref E.Uncacheable);
                if (weight < 60 && IsSwimmingDepth())
                {
                    if (E.Smart)
                    {
                        E.Uncacheable = true;
                        int penalty = Swimming.BASE_MOVE_SPEED_PENALTY;
                        GetSwimmingPerformanceEvent.GetFor(E.Actor, ref penalty);
                        int adjust = penalty / 10;
                        if (adjust != 0 && E.Actor != null && E.Actor.IsPlayer())
                        {
                            adjust *= 2;
                        }
                        weight = Math.Min(weight + adjust, 60);
                    }
                    else
                    {
                        weight = Math.Min(weight + (E.Swimming ? 2 : 5), 60);
                    }
                }
                else
                if (weight < 30 && IsWadingDepth())
                {
                    weight = Math.Min(weight + 1, 30);
                }
                if (E.Reefer && weight < Zone.REEFER_NAV_WEIGHT && ContainsLiquid(LiquidAlgae.ID) && IsWadingDepth())
                {
                    weight = Zone.REEFER_NAV_WEIGHT;
                }
                E.MinWeight(weight);
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(BeforeRenderEvent E)
        {
            if (Primary != null)
            {
                RequirePrimaryLiquid().BeforeRender(this);
            }
            if (Secondary != null)
            {
                RequireSecondaryLiquid().BeforeRenderSecondary(this);
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetFreeDramsEvent E)
        {
            if (E.ApplyTo(ParentObject) && !EffectivelySealed())
            {
                if (IsPureLiquid(E.Liquid))
                {
                    E.Drams += Volume;
                }
                else
                if (E.ImpureOkay && ComponentLiquids.ContainsKey(E.Liquid))
                {
                    E.Drams += Math.Max(Volume * ComponentLiquids[E.Liquid] / 1000, 1);
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetStorableDramsEvent E)
        {
            if (Volume < MaxVolume && E.ApplyTo(ParentObject))
            {
                if (
                    (IsPureLiquid(E.Liquid) || (IsEmpty() && (AutoCollectLiquidType == null || AutoCollectLiquidType == E.Liquid)))
                    && !EffectivelySealed()
                    && ParentObject.AllowLiquidCollection(E.Liquid, actor: E.Actor)
                )
                {
                    E.Drams += MaxVolume - Volume;
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GiveDramsEvent E)
        {
            if (Volume < MaxVolume && E.Drams > 0 && E.ApplyTo(ParentObject))
            {
                if (
                    (E.Pass == 1 && AutoCollectLiquidType == E.Liquid)
                    || (E.Pass >= 2 && ParentObject.WantsLiquidCollection(E.Liquid))
                    || (E.Pass >= 3 && IsPureLiquid(E.Liquid) && !ProducesLiquidEvent.Check(ParentObject, E.Liquid))
                    || (E.Pass >= 4 && IsEmpty() && !ProducesLiquidEvent.Check(ParentObject, E.Liquid))
                    || E.Pass >= 5
                )
                {
                    int newDrams = E.Drams;
                    GiveDrams(E.Liquid, ref newDrams, E.Auto, E.StoredIn, E.Actor);
                    if (E.Drams != newDrams)
                    {
                        E.Drams = newDrams;
                        if (E.Drams <= 0)
                        {
                            return false;
                        }
                    }
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(UseDramsEvent E)
        {
            if (
                Volume > 0
                && E.Drams > 0
                && E.ApplyTo(ParentObject)
                && (
                    (E.Pass == 1 && ProducesLiquidEvent.Check(ParentObject, E.Liquid))
                    || (E.Pass == 2 && !ParentObject.WantsLiquidCollection(E.Liquid))
                    || E.Pass >= 3
                )
                && !EffectivelySealed()
            )
            {
                if (IsPureLiquid(E.Liquid))
                {
                    if (Volume >= E.Drams)
                    {
                        Volume -= E.Drams;
                        E.Drams = 0;
                    }
                    else
                    {
                        E.Drams -= Volume;
                        Volume = 0;
                    }
                    if (E.TrackContainers != null && !E.TrackContainers.Contains(ParentObject))
                    {
                        E.TrackContainers.Add(ParentObject);
                    }
                    if (Volume <= 0)
                    {
                        Empty();
                    }
                    else
                    {
                        FlushWeightCaches();
                        CheckImage();
                    }
                    if (E.Drams <= 0)
                    {
                        return false;
                    }
                }
                else
                if (E.ImpureOkay && ComponentLiquids.ContainsKey(E.Liquid))
                {
                    int Available = Math.Max(Volume * ComponentLiquids[E.Liquid] / 1000, 1);
                    int Consume;
                    if (Available >= E.Drams)
                    {
                        Consume = E.Drams;
                        E.Drams = 0;
                    }
                    else
                    {
                        Consume = Available;
                        E.Drams -= Available;
                    }
                    UseDrams(E.Liquid, Consume);
                    if (E.TrackContainers != null && !E.TrackContainers.Contains(ParentObject))
                    {
                        E.TrackContainers.Add(ParentObject);
                    }
                    if (E.Drams <= 0)
                    {
                        return false;
                    }
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(AnyAutoCollectDramsEvent E)
        {
            if (Volume < MaxVolume && E.ApplyTo(ParentObject))
            {
                string AutogetLiquid = GetActiveAutogetLiquid();
                if (
                    AutogetLiquid != null
                    && E.Liquid == AutogetLiquid
                    && (IsPureLiquid(E.Liquid) || IsEmpty())
                    && !EffectivelySealed()
                    && ParentObject.AllowLiquidCollection(E.Liquid, actor: E.Actor)
                )
                {
                    return false; // positive case
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetAutoCollectDramsEvent E)
        {
            if (Volume < MaxVolume && E.ApplyTo(ParentObject))
            {
                string AutogetLiquid = GetActiveAutogetLiquid();
                if (
                    AutogetLiquid != null
                    && E.Liquid == AutogetLiquid
                    && (IsPureLiquid(E.Liquid) || IsEmpty())
                    && !EffectivelySealed()
                    && ParentObject.AllowLiquidCollection(E.Liquid, actor: E.Actor)
                )
                {
                    E.Drams += MaxVolume - Volume;
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(AllowLiquidCollectionEvent E)
        {
            if (AutoCollectLiquidType != null && E.Liquid != AutoCollectLiquidType)
            {
                return false;
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(EnteredCellEvent E)
        {
            if (E.Cell != null)
            {
                List<string> keylist = getKeylist();
                try
                {
                    keylist.Clear();
                    keylist.AddRange(ComponentLiquids.Keys);
                    foreach (string l in keylist)
                    {
                        if (!Liquids[l].EnteredCell(this, E))
                        {
                            return false;
                        }
                    }
                    if (IsOpenVolume())
                    {
                        for (int i = 0, j = E.Cell.Objects.Count; i < j; i++)
                        {
                            GameObject obj = E.Cell.Objects[i];
                            if (obj != ParentObject)
                            {
                                LiquidVolume LV = obj.LiquidVolume;
                                if (LV != null && LV.IsOpenVolume())
                                {
                                    Temporary pTemporary = ParentObject.GetPart<Temporary>();
                                    ExistenceSupport pEx = ParentObject.GetPart<ExistenceSupport>();
                                    if (pTemporary != null)
                                    {
                                        pTemporary.Expire(Silent: true);
                                        if (GameObject.Validate(ParentObject))
                                        {
                                            Empty();
                                        }
                                    }
                                    else
                                    if (pEx != null)
                                    {
                                        pEx.Unsupported(Silent: true);
                                        if (GameObject.Validate(ParentObject))
                                        {
                                            Empty();
                                        }
                                    }
                                    else
                                    {
                                        ParentObject.RemoveFromContext();
                                        LV.MixWith(
                                            Liquid: this,
                                            PouredFrom: ParentObject,
                                            RequestInterfaceExit: ref E.InterfaceExit
                                        );
                                        ParentObject.Obliterate();
                                    }
                                    return false;
                                }
                            }
                        }
                        if (
                            E.Type != "Pour"
                            && E.Type != "Flow"
                            && E.Cell.Objects.Count > 1
                            && E.Cell.ParentZone.IsActive()
                            && CanInteractWithAnything(E.Cell)
                        )
                        {
                            for (int i = 0, j = E.Cell.Objects.Count; i < j; i++)
                            {
                                GameObject GO = E.Cell.Objects[i];
                                if (CanInteractWith(GO, E.Cell, AnythingChecked: true))
                                {
                                    foreach (string b in keylist)
                                    {
                                        Liquids[b].ObjectInCell(this, GO);
                                    }
                                    ProcessContact(GO, Initial: true, Prone: GO.HasEffect<Effects.Prone>(), FromCell: true);
                                }
                                if (j != E.Cell.Objects.Count)
                                {
                                    j = E.Cell.Objects.Count;
                                    if (i < j && E.Cell.Objects[i] != GO)
                                    {
                                        i--;
                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {
                    keylistPool.Enqueue(keylist);
                }
            }
            return base.HandleEvent(E);
        }

        public void Splash(Cell C = null)
        {
            if (C == null)
            {
                if (ParentObject != null)
                {
                    C = ParentObject.CurrentCell;
                }
                if (C == null)
                {
                    return;
                }
            }
            if (C.IsVisible())
            {
                if (Secondary != null && 10.in100())
                {
                    C.LiquidSplash(RequireSecondaryLiquid());
                }
                else
                if (Primary != null)
                {
                    C.LiquidSplash(RequirePrimaryLiquid());
                }
            }
            string Sound = null;
            if (Primary != null)
            {
                string sound = RequirePrimaryLiquid().SplashSound(this);
                if (sound != null)
                {
                    Sound = sound;
                }
            }
            if (Secondary != null && Sound == null)
            {
                string sound = RequireSecondaryLiquid().SplashSound(this);
                if (sound != null)
                {
                    Sound = sound;
                }
            }
            if (Sound == null)
            {
                List<string> Tertiaries = GetTertiaries();
                if (Tertiaries != null)
                {
                    foreach (string Liquid in Tertiaries)
                    {
                        if (Liquid != Primary && Liquid != Secondary)
                        {
                            string sound = Liquids[Liquid].SplashSound(this);
                            if (sound != null)
                            {
                                Sound = sound;
                                break;
                            }
                        }
                    }
                }
            }
            PlayWorldSound(Sound, 0.5f, SourceCell: C);
        }

        [NonSerialized] private static List<BodyPart> BodyParts = new List<BodyPart>();
        [NonSerialized] private static List<BodyPart> MobilityBodyParts = new List<BodyPart>();
        [NonSerialized] private static Dictionary<BodyPart, int> BodyPartCapacity = new Dictionary<BodyPart, int>();
        [NonSerialized] private static Dictionary<BodyPart, int> BodyPartExposure = new Dictionary<BodyPart, int>();

        private int GetCurrentCoverIfSame(GameObject obj)
        {
            LiquidCovered cover = obj.GetEffect<LiquidCovered>();
            if (cover != null && LiquidSameAs(cover.Liquid))
            {
                return cover.Liquid.Volume;
            }
            return 0;
        }

        private void LiquidInContact(
            GameObject obj,
            int Amount,
            ref bool TemporaryKnown,
            ref bool Temporary,
            bool Poured,
            GameObject PouredBy,
            bool FromCell
        )
        {
            if (!TemporaryKnown)
            {
                TemporaryKnown = true;
                Temporary = ParentObject.IsTemporary;
            }
            if (Temporary)
            {
                int realVolume = Volume;
                Volume = Amount;
                try
                {
                    SmearOn(obj, PouredBy, FromCell);
                }
                finally
                {
                    Volume = realVolume;
                }
            }
            else
            {
                obj.ApplyEffect(new LiquidCovered(this, Amount, Poured: Poured, PouredBy: PouredBy, FromCell: FromCell));
            }
        }

        public int ProcessContact(
            GameObject obj,
            bool Initial = false,
            bool Prone = false,
            bool Poured = false,
            GameObject PouredBy = null,
            bool FromCell = false,
            int ContactVolume = -1
        )
        {
            if (ParentObject != null)
            {
                if (ParentObject.IsFrozen())
                {
                    return 0;
                }
                if (ParentObject.IsInStasis())
                {
                    return 0;
                }
            }
            if (obj.Physics?.IsReal != true)
            {
                return 0;
            }
            if (obj.IsScenery)
            {
                return 0;
            }
            if (FromCell)
            {
                LiquidCovered cover = obj.GetEffect<LiquidCovered>();
                if (cover != null && cover.FromCell)
                {
                    if (LiquidSameAs(cover.Liquid))
                    {
                        return 0;
                    }
                    cover.FromCell = false;
                }
            }
            int StartVolume = Volume;
            if (ContactVolume == -1)
            {
                ContactVolume = Volume;
            }
            bool WantSplash = false;
            int MaxExposure = obj.GetMaximumLiquidExposure();
            if (MaxExposure <= 0)
            {
                return 0;
            }
            Body pBody = obj.Body;
            List<GameObject> Equipment = null;
            if (pBody != null)
            {
                Equipment = Event.NewGameObjectList();
                pBody.GetEquippedObjectsExceptNatural(Equipment);
            }
            bool TempKnown = false;
            bool Temp = false;
            if (!Poured && IsSwimmableFor(obj))
            {
                WantSplash = true;
                if (Initial && obj.IsPlayer())
                {
                    XDidYToZ(obj, "swim", "through", ParentObject, IndefiniteObject: true);
                }
                if (obj.IsCreature && !obj.HasEffect<Effects.Swimming>() && obj.IsPotentiallyMobile())
                {
                    obj.ApplyEffect(new Swimming());
                }
                int CutoffVolume = StartVolume / 2;
                int Amount = Math.Min(Math.Min(MaxExposure, CutoffVolume), ContactVolume);
                if (Amount > 0)
                {
                    int UseAmount = Amount - GetCurrentCoverIfSame(obj);
                    if (UseAmount > 0)
                    {
                        LiquidInContact(obj, UseAmount, ref TempKnown, ref Temp, Poured, PouredBy, FromCell);
                    }
                    WantSplash = true;
                    ContactVolume -= Amount;
                }
                if (Equipment != null && Equipment.Count > 0 && Volume >= CutoffVolume && ContactVolume > 0)
                {
                    if (Equipment.Count > 1)
                    {
                        Equipment.ShuffleInPlace();
                    }
                    for (int i = 0, j = Equipment.Count; i < j; i++)
                    {
                        Amount = Math.Min(Math.Min(GetAdsorbableDrams(Equipment[i]), Volume / 2), ContactVolume);
                        if (Amount > 0)
                        {
                            int UseAmount = Amount - GetCurrentCoverIfSame(Equipment[i]);
                            if (UseAmount > 0)
                            {
                                LiquidInContact(Equipment[i], UseAmount, ref TempKnown, ref Temp, Poured, PouredBy, FromCell);
                            }
                            WantSplash = true;
                            ContactVolume -= Amount;
                        }
                        if (Volume < CutoffVolume || ContactVolume <= 0)
                        {
                            break;
                        }
                    }
                }
                if (Volume >= CutoffVolume && ContactVolume > 0)
                {
                    Inventory pInventory = obj.Inventory;
                    List<GameObject> Inventory = null;
                    if (pInventory != null)
                    {
                        Inventory = Event.NewGameObjectList();
                        Inventory.AddRange(pInventory.Objects);
                    }
                    if (Inventory != null && Inventory.Count > 0)
                    {
                        if (Inventory.Count > 1)
                        {
                            Inventory.ShuffleInPlace();
                        }
                        for (int i = 0, j = Inventory.Count; i < j; i++)
                        {
                            if (Inventory[i].Weight == 0)
                            {
                                continue;
                            }
                            Amount = Math.Min(Math.Min(GetAdsorbableDrams(Inventory[i]), Volume / 2), ContactVolume);
                            if (Amount > 0)
                            {
                                int UseAmount = Amount - GetCurrentCoverIfSame(Inventory[i]);
                                if (UseAmount > 0)
                                {
                                    LiquidInContact(Inventory[i], UseAmount, ref TempKnown, ref Temp, Poured, PouredBy, FromCell);
                                }
                                WantSplash = true;
                                ContactVolume -= Amount;
                            }
                            if (Volume < CutoffVolume || ContactVolume <= 0)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                if( !Poured && Volume > 0 ) WantSplash = true;
                if (!Poured && IsWadingDepth())
                {
                    if (Initial && obj.IsPlayer())
                    {
                        XDidYToZ(obj, "wade", "through", ParentObject, IndefiniteObject: true);
                    }
                    if (obj.IsCreature && !obj.HasEffect<Effects.Wading>() && obj.IsPotentiallyMobile())
                    {
                        obj.ApplyEffect(new Wading());
                    }
                }
                double ExposureProportion;
                int MinBodyExposure = Poured ? Math.Max(ContactVolume / 2, 1) : 0;
                int Surface;
                if (Poured)
                {
                    ExposureProportion = 1;
                    Surface = ContactVolume;
                }
                else
                {
                    if (pBody != null)
                    {
                        if (Prone)
                        {
                            ExposureProportion = 0.4;
                        }
                        else
                        if (pBody.GetBodyMobility() > 0 || pBody.GetTotalMobility() <= 0)
                        {
                            ExposureProportion = 0.3;
                        }
                        else
                        {
                            ExposureProportion = 0.1;
                        }
                    }
                    else
                    {
                        ExposureProportion = 0.2;
                    }
                    ExposureProportion *= (1 + ContactVolume / 300.0);
                    if (ExposureProportion >= 1)
                    {
                        ExposureProportion = 1;
                        Surface = ContactVolume;
                    }
                    else
                    {
                        Surface = (int) (ContactVolume * ExposureProportion);
                    }
                }
                int Chance = Poured ? 100 : ContactVolume + Surface;
                int Exposure;
                if (Chance >= 100)
                {
                    Exposure = Math.Min(ContactVolume, Surface);
                }
                else
                {
                    Exposure = 0;
                    for (int i = 0; i < ContactVolume; i++)
                    {
                        if (Chance.in100())
                        {
                            Exposure++;
                            if (Exposure >= Surface)
                            {
                                break;
                            }
                        }
                    }
                }
                if (Exposure > 0)
                {
                    int MainAdsorb = GetAdsorbableDrams(obj);
                    Exposure -= MinBodyExposure;
                    if (pBody != null)
                    {
                        BodyParts.Clear();
                        pBody.GetConcreteParts(BodyParts);
                        BodyPartExposure.Clear();
                        BodyPartCapacity.Clear();
                        BodyPartExposure.Clear();
                        int FullBodyChance = 0;
                        int Assigned = 0;
                        if (!Poured)
                        {
                            MobilityBodyParts.Clear();
                            pBody.GetConcreteMobilityProvidingParts(MobilityBodyParts);
                            if (MobilityBodyParts.Count > 0)
                            {
                                if (MobilityBodyParts.Count > 1)
                                {
                                    MobilityBodyParts.ShuffleInPlace();
                                }
                                for (int i = 0, j = MobilityBodyParts.Count; i < j; i++)
                                {
                                    BodyPart Part = MobilityBodyParts[i];
                                    if (Part.Equipped != null)
                                    {
                                        int Max = GetAdsorbableDrams(Part.Equipped);
                                        if (Max > 0)
                                        {
                                            BodyPartCapacity.Add(Part, Max);
                                        }
                                    }
                                    else
                                    {
                                        BodyPartCapacity.Add(Part, Math.Max(MaxExposure / BodyParts.Count, 1));
                                    }
                                }
                                int RandomizerTotal = 0;
                                for (int i = 0; i < Exposure; i++)
                                {
                                    BodyPart Part = BodyPartCapacity.GetRandomElement(ref RandomizerTotal);
                                    if (Part == null)
                                    {
                                        break;
                                    }
                                    int Current;
                                    if (BodyPartExposure.TryGetValue(Part, out Current))
                                    {
                                        if (Current < BodyPartCapacity[Part])
                                        {
                                            BodyPartExposure[Part]++;
                                            Assigned++;
                                        }
                                    }
                                    else
                                    {
                                        BodyPartExposure.Add(Part, 1);
                                        Assigned++;
                                    }
                                }
                                if (Exposure > Assigned && BodyParts.Count > MobilityBodyParts.Count)
                                {
                                    FullBodyChance = (Volume - 100) / 2;
                                }
                            }
                            else
                            {
                                FullBodyChance = 100;
                            }
                        }
                        else
                        {
                            FullBodyChance = 100;
                        }
                        if (FullBodyChance > 0)
                        {
                            int FullBodyExposure = 0;
                            if (FullBodyChance >= 100)
                            {
                                FullBodyExposure = Exposure;
                            }
                            else
                            {
                                for (int i = Assigned; i < Exposure; i++)
                                {
                                    if (FullBodyChance.in100())
                                    {
                                        FullBodyExposure++;
                                    }
                                }
                            }
                            if (FullBodyExposure > 0)
                            {
                                for (int i = 0, j = BodyParts.Count; i < j; i++)
                                {
                                    BodyPart Part = BodyParts[i];
                                    if (!BodyPartCapacity.ContainsKey(Part))
                                    {
                                        if (Part.Equipped != null)
                                        {
                                            int Max = GetAdsorbableDrams(Part.Equipped);
                                            if (Max > 0)
                                            {
                                                BodyPartCapacity.Add(Part, Max);
                                            }
                                        }
                                        else
                                        {
                                            BodyPartCapacity.Add(Part, Math.Max(MaxExposure / BodyParts.Count, 1));
                                        }
                                    }
                                }
                                int RandomizerTotal = 0;
                                for (int i = 0; i < FullBodyExposure; i++)
                                {
                                    BodyPart Part = BodyPartCapacity.GetRandomElement(ref RandomizerTotal);
                                    if (Part == null)
                                    {
                                        break;
                                    }
                                    int Current;
                                    if (BodyPartExposure.TryGetValue(Part, out Current))
                                    {
                                        if (Current < BodyPartCapacity[Part])
                                        {
                                            BodyPartExposure[Part]++;
                                            Assigned++;
                                        }
                                    }
                                    else
                                    {
                                        BodyPartExposure.Add(Part, 1);
                                        Assigned++;
                                    }
                                }
                            }
                        }
                        foreach (KeyValuePair<BodyPart, int> KV in BodyPartExposure)
                        {
                            if (KV.Key.Equipped != null && !KV.Key.Equipped.HasTag("NaturalGear"))
                            {
                                Exposure -= KV.Value;
                                int UseAmount = KV.Value - GetCurrentCoverIfSame(KV.Key.Equipped);
                                if (UseAmount > 0)
                                {
                                    LiquidInContact(KV.Key.Equipped, UseAmount, ref TempKnown, ref Temp, Poured, PouredBy, FromCell);
                                }
                            }
                        }
                    }
                    Exposure += MinBodyExposure;
                    if (Exposure > 0)
                    {
                        int UseAmount = Math.Min(Exposure - GetCurrentCoverIfSame(obj), MainAdsorb);
                        if (UseAmount > 0)
                        {
                            LiquidInContact(obj, UseAmount, ref TempKnown, ref Temp, Poured, PouredBy, FromCell);
                        }
                    }
                    WantSplash = true;
                }
            }
            if (WantSplash && obj.IsPlayer() && !AutoAct.IsActive(IgnoreAutoget: true))
            {
                Splash(obj.CurrentCell);
            }
            return StartVolume - Volume;
        }

        public override bool HandleEvent(ObjectEnteredCellEvent E)
        {
            GameObject GO = E.Object;
            if (!E.IgnoreGravity && CanInteractWith(GO, E.Cell))
            {
                List<string> keylist = getKeylist();
                try
                {
                    keylist.Clear();
                    foreach (KeyValuePair<string, int> pair in ComponentLiquids)
                    {
                        keylist.Add(pair.Key);
                    }
                    foreach (string name in keylist)
                    {
                        BaseLiquid Liquid = Liquids[name];
                        Liquid.ObjectEnteredCell(this, E);
                        #pragma warning disable 0618
                        Liquid.ObjectEnteredCell(this, GO);
                        #pragma warning restore
                    }
                    if (IsOpenVolume())
                    {
                        ProcessContact(GO, Initial: true, FromCell: true);
                    }
                }
                finally
                {
                    keylistPool.Enqueue(keylist);
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GravitationEvent E)
        {
            GameObject GO = E.Object;
            if (CanInteractWith(GO, E.Cell))
            {
                List<string> keylist = getKeylist();
                try
                {
                    keylist.Clear();
                    foreach (KeyValuePair<string, int> pair in ComponentLiquids)
                    {
                        keylist.Add(pair.Key);
                    }
                    foreach (string name in keylist)
                    {
                        BaseLiquid Liquid = Liquids[name];
                        Liquid.ObjectEnteredCell(this, E);
                        #pragma warning disable 0618
                        Liquid.ObjectEnteredCell(this, GO);
                        #pragma warning restore
                    }
                    if (IsOpenVolume())
                    {
                        ProcessContact(GO, Initial: true, FromCell: true);
                    }
                }
                finally
                {
                    keylistPool.Enqueue(keylist);
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(ObjectGoingProneEvent E)
        {
            GameObject GO = E.Object;
            if (CanInteractWith(GO, E.Cell))
            {
                List<string> keylist = getKeylist();
                try
                {
                    keylist.Clear();
                    keylist.AddRange(ComponentLiquids.Keys);
                    foreach (string b in keylist)
                    {
                        Liquids[b].ObjectGoingProne(this, E.Object, UsePopups: E.UsePopups);
                    }
                    if (IsOpenVolume())
                    {
                        ProcessContact(GO, Prone: true, FromCell: true);
                    }
                }
                finally
                {
                    keylistPool.Enqueue(keylist);
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(ObjectStoppedFlyingEvent E)
        {
            GameObject GO = E.Object;
            if (CanInteractWith(GO, E.Cell))
            {
                List<string> keylist = getKeylist();
                try
                {
                    keylist.Clear();
                    keylist.AddRange(ComponentLiquids.Keys);
                    foreach (string b in keylist)
                    {
                        Liquids[b].ObjectInCell(this, E.Object);
                    }
                    if (IsOpenVolume())
                    {
                        ProcessContact(GO, Initial: true, FromCell: true);
                    }
                }
                finally
                {
                    keylistPool.Enqueue(keylist);
                }
            }
            return base.HandleEvent(E);
        }

        public void ProcessExposure(GameObject GO, GameObject By = null, bool FromCell = false)
        {
            if (!GameObject.Validate(ref GO))
            {
                return;
            }
            double exposedTo = Math.Min(Volume, GO.GetMaximumLiquidExposureAsDouble());
            if (exposedTo <= 0)
            {
                return;
            }
            int ThermalConductivity = GetLiquidThermalConductivity();
            if (ThermalConductivity <= 0)
            {
                return;
            }
            int Temperature = GetLiquidTemperature();
            int objTemp = GO.Physics.Temperature;
            int tempDiff = Temperature - objTemp;
            if (tempDiff == 0)
            {
                return;
            }
            if (ParentObject != null && !GO.PhaseMatches(ParentObject))
            {
                return;
            }
            int Combustibility = GetLiquidCombustibility();
            bool onFire = GO.IsAflame();
            if (onFire && Combustibility >= 50)
            {
                GO.TemperatureChange((int) (exposedTo.DiminishingReturns(8) * Combustibility / 50), Actor: By);
                return;
            }
            double tempInfluence = tempDiff * exposedTo.DiminishingReturns(8) / 4;
            if (tempDiff > 0)
            {
                if (tempInfluence > tempDiff)
                {
                    tempInfluence = (float) tempDiff;
                }
            }
            else
            {
                if (tempInfluence < tempDiff)
                {
                    tempInfluence = (float) tempDiff;
                }
            }
            if (onFire && Combustibility != 0)
            {
                if (tempInfluence > 0)
                {
                    tempInfluence = tempInfluence * (100 + Combustibility) / 100;
                }
                else
                {
                    tempInfluence = tempInfluence * (100 - Combustibility) / 100;
                }
            }
            if (ThermalConductivity != 100)
            {
                tempInfluence = tempInfluence * ThermalConductivity / 100;
            }
            if (tempInfluence > 0)
            {
                GO.TemperatureChange((int) tempInfluence, Actor: By, Max: Temperature);
            }
            else
            {
                GO.TemperatureChange((int) tempInfluence, Actor: By, Min: Temperature);
            }
        }

        public override bool HandleEvent(GetGameObjectSortEvent E)
        {
            if (E.Category1 == "Water Containers" && E.Category2 == "Water Containers")
            {
                LiquidVolume vol1 = E.Object1?.LiquidVolume;
                LiquidVolume vol2 = E.Object2?.LiquidVolume;
                int missingCompare = (vol2 != null).CompareTo(vol1 != null);
                if (missingCompare != 0)
                {
                    E.Sort = missingCompare;
                    return false;
                }
                if (vol1 != null && vol2 != null)
                {
                    int emptyCompare = vol1.IsEmpty().CompareTo(vol2.IsEmpty());
                    if (emptyCompare != 0)
                    {
                        E.Sort = emptyCompare;
                        return false;
                    }
                    int liquidValueCompare = vol2.GetLiquidExtrinsicValuePerDram().CompareTo(vol1.GetLiquidExtrinsicValuePerDram());
                    if (liquidValueCompare != 0)
                    {
                        E.Sort = liquidValueCompare;
                        return false;
                    }
                    int liquidNameCompare = vol1.GetLiquidName().CompareTo(vol2.GetLiquidName());
                    if (liquidNameCompare != 0)
                    {
                        E.Sort = liquidNameCompare;
                        return false;
                    }
                    int volumeCompare = vol2.Volume.CompareTo(vol1.Volume);
                    if (volumeCompare != 0)
                    {
                        E.Sort = volumeCompare;
                        return false;
                    }
                }
                int nameCompare = string.Compare(E.Object1?.GetDisplayName(Stripped: true), E.Object2?.GetDisplayName(Stripped: true), ignoreCase: true);
                if (nameCompare != 0)
                {
                    E.Sort = nameCompare;
                    return false;
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetDisplayNameEvent E)
        {
            if (E.ForSort && ParentObject.GetInventoryCategory() == "Water Containers")
            {
                E.ReplaceEntirety("Water Containers");
                return false;
            }
            if (E.GetPrimaryBase() == "pool")
            {
                if (IsSwimmingDepth())
                {
                    E.AddAdjective("deep");
                }
                else
                if (!IsWadingDepth())
                {
                    E.ReplacePrimaryBase("puddle");
                }
            }
            if (!E.Reference && (E.AsIfKnown || GetEpistemicStatus() != Examiner.EPISTEMIC_STATUS_UNKNOWN))
            {
                if (IsEmpty())
                {
                    if (!UsesNamePreposition())
                    {
                        if (EffectivelySealed())
                        {
                            if (LiquidVisibleWhenSealed)
                            {
                                if (ShowSeal)
                                {
                                    if (E.NoColor)
                                    {
                                        E.AddTag("[empty, sealed]", DescriptionBuilder.ORDER_ADJUST_SLIGHTLY_EARLY);
                                    }
                                    else
                                    {
                                        E.AddTag("{{y|[{{K|empty, {{c|sealed}}}}]}}", DescriptionBuilder.ORDER_ADJUST_SLIGHTLY_EARLY);
                                    }
                                }
                                else
                                {
                                    if (E.NoColor)
                                    {
                                        E.AddTag("[empty]", DescriptionBuilder.ORDER_ADJUST_SLIGHTLY_EARLY);
                                    }
                                    else
                                    {
                                        E.AddTag("{{y|[{{K|empty}}]}}", DescriptionBuilder.ORDER_ADJUST_SLIGHTLY_EARLY);
                                    }
                                }
                            }
                            else
                            {
                                if (ShowSeal)
                                {
                                    if (E.NoColor)
                                    {
                                        E.AddTag("[sealed]", DescriptionBuilder.ORDER_ADJUST_SLIGHTLY_EARLY);
                                    }
                                    else
                                    {
                                        E.AddTag("{{y|[{{c|sealed}}]}}", DescriptionBuilder.ORDER_ADJUST_SLIGHTLY_EARLY);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (E.NoColor)
                            {
                                E.AddTag("[empty]", DescriptionBuilder.ORDER_ADJUST_SLIGHTLY_EARLY);
                            }
                            else
                            {
                                E.AddTag("{{y|[{{K|empty}}]}}", DescriptionBuilder.ORDER_ADJUST_SLIGHTLY_EARLY);
                            }
                        }
                    }
                }
                else
                {
                    string desc = GetLiquidDescription(
                        IncludeAmount: (E.Cutoff > DescriptionBuilder.SHORT_CUTOFF),
                        Stripped: E.NoColor,
                        ForSort: E.ForSort
                    );
                    if (!desc.IsNullOrEmpty())
                    {
                        if (UsesNamePreposition())
                        {
                            E.AddClause(desc);
                        }
                        else
                        {
                            E.AddTag(desc, DescriptionBuilder.ORDER_ADJUST_SLIGHTLY_EARLY);
                        }
                    }
                }
                if (AutoCollectLiquidType != null)
                {
                    if (Volume == 0 || !IsPureLiquid(AutoCollectLiquidType))
                    {
                        if( Liquids.ContainsKey(AutoCollectLiquidType) && GetLiquid(AutoCollectLiquidType) != null )
                        {
                            if( E.NoColor )
                            {
                                E.AddTag("[auto-collecting " + Liquids[AutoCollectLiquidType].GetName(this).Strip() + "]", DescriptionBuilder.ORDER_ADJUST_LATE);
                            }
                            else
                            {
                                E.AddTag("{{y|[{{c|auto-collecting " + Liquids[AutoCollectLiquidType].GetName(this) + "}}]}}", DescriptionBuilder.ORDER_ADJUST_LATE);
                            }
                        }
                        else
                        {
                            MetricsManager.LogEditorWarning($"unknown AutoCollectLiquidType {AutoCollectLiquidType}");
                        }
                    }
                    else
                    {
                        if (E.NoColor)
                        {
                            E.AddTag("[auto-collecting]", DescriptionBuilder.ORDER_ADJUST_LATE);
                        }
                        else
                        {
                            E.AddTag("{{y|[{{c|auto-collecting}}]}}", DescriptionBuilder.ORDER_ADJUST_LATE);
                        }
                    }
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            if (ParentObject.HasTagOrProperty("Pool") && Volume > 0)
            {
                StringBuilder description = new StringBuilder();
                if (IsSwimmingDepth())
                {
                    description.Append("Light scatters off the surface of the wide and yawning pool and gets lost in its depths.");
                }
                else if (IsWadingDepth())
                {
                    description.Append("Liquid pools into a cistern of air and ground.");
                }
                else
                {
                    description.Append("Liquid dots the ground in shallow pools.");
                }

                E.Base = description;
            }

            if (Volume > 0 && (!EffectivelySealed() || LiquidVisibleWhenSealed))
            {
                List<string> cookingTypes = null;
                foreach (var t in ComponentLiquids)
                {
                    if (t.Value > 0)
                    {
                        List<string> types = Liquids[t.Key].GetPreparedCookingIngredient().CachedCommaExpansion();
                        if (cookingTypes == null)
                        {
                            cookingTypes = types.ToList();
                        }
                        else
                        {
                            for (int i = 0, j = types.Count; i < j; i++)
                            {
                                if (!cookingTypes.Contains(types[i]))
                                {
                                    cookingTypes.Add(types[i]);
                                }
                            }
                        }
                    }
                }
                if (
                    cookingTypes != null
                    && (
                        IsOpenVolume()
                        || (ParentObject != null && ParentObject.HasTagOrProperty("WaterContainer"))
                    )
                )
                {
                    List<string> ingredientTypes = null;
                    foreach (string type in cookingTypes)
                    {
                        string bp = Campfire.COOKING_PREFIX + type;
                        if (GameObjectFactory.Factory.Blueprints.ContainsKey(bp))
                        {
                            string desc = GameObjectFactory.Factory.Blueprints[bp].GetTag("Description");
                            if (!desc.IsNullOrEmpty())
                            {
                                if (ingredientTypes == null)
                                {
                                    ingredientTypes = new List<string>();
                                }
                                ingredientTypes.Add(desc);
                            }
                        }
                    }
                    if (ingredientTypes != null)
                    {
                        E.Postfix.AppendRules(
                            "Adds "
                            + XRL.Language.Grammar.MakeOrList(ingredientTypes)
                            + " effects to cooked meals."
                        );
                    }
                }
            }
            if (Sealed)
            {
                E.Postfix.AppendRules("Sealed: The liquid contained inside can't be accessed.");
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetDebugInternalsEvent E)
        {
            StringBuilder SB = Event.NewStringBuilder();
            foreach (KeyValuePair<string, int> KV in ComponentLiquids)
            {
                SB
                    .Compound(KV.Key, '\n')
                    .Append('-')
                    .Append(KV.Value)
                ;
            }
            E.AddEntry(this, "ComponentLiquids", SB.ToString());
            E.AddEntry(this, "Primary", Primary);
            E.AddEntry(this, "Secondary", Secondary);
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetSlottedInventoryActionsEvent E)
        {
            if (GetEpistemicStatus() != Examiner.EPISTEMIC_STATUS_UNKNOWN && !EffectivelySealed())
            {
                E.AddAction(
                    Name: "FillSlotted",
                    Display: "fill " + ParentObject.BaseDisplayNameStripped,
                    Command: "FillFrom",
                    Key: 'f',
                    Default: 50 - (Volume * 50 / MaxVolume),
                    FireOn: ParentObject
                );
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetInventoryActionsAlwaysEvent E)
        {
            int epistemicStatus = GetEpistemicStatus();
            if (epistemicStatus != Examiner.EPISTEMIC_STATUS_UNKNOWN)
            {
                if (ManualSeal)
                {
                    if (Sealed)
                    {
                        E.AddAction(
                            Name: "Unseal",
                            Display: "unseal",
                            Command: "Unseal",
                            Key: 'u',
                            WorksTelekinetically: true
                        );
                    }
                    else
                    {
                        E.AddAction(
                            Name: "Seal",
                            Display: "seal",
                            Command: "Seal",
                            Key: 's',
                            WorksTelekinetically: true
                        );
                    }
                }
                if (!EffectivelySealed())
                {
                    if (!IsOpenVolume())
                    {
                        E.AddAction(
                            Name: "Fill",
                            Display: "fill",
                            Command: "FillFrom",
                            Key: 'f'
                        );
                        if (HasDrain)
                        {
                            E.AddAction(
                                Name: "Drain",
                                Display: "drain",
                                Command: "Drain",
                                Key: 'r',
                                Default: -1
                            );
                        }
                    }
                    if (Volume > 0)
                    {
                        int PourPriority = 0;
                        if ((ParentObject.Equipped != null || ParentObject.InInventory != null) && !ParentObject.HasPart<MissileWeapon>())
                        {
                            PourPriority = 20;
                        }
                        E.AddAction("Drink", "drink", "Drink", Key: 'k');
                        E.AddAction("Pour", "pour", "Pour", Key: 'p', Default: PourPriority);
                        bool WantAutoCollect = false;
                        bool WantCollect = false;
                        int CollectPriority = 10;
                        if (ParentObject.InInventory == E.Actor)
                        {
                            WantAutoCollect = true;
                        }
                        else
                        {
                            WantCollect = true;
                            if (ParentObject.Equipped == E.Actor)
                            {
                                WantAutoCollect = true;
                                CollectPriority = -1;
                            }
                        }
                        if (WantAutoCollect && epistemicStatus == Examiner.EPISTEMIC_STATUS_KNOWN)
                        {
                            if (AutoCollectLiquidType != null)
                            {
                                E.AddAction("AutoCollect", "stop auto-collecting liquid", "AutoCollectLiquid", Key: 'a', Default: -1);
                            }
                            else
                            {
                                E.AddAction("AutoCollect", "auto-collect liquid", "AutoCollectLiquid", Key: 'a', Default: -1);
                            }
                        }
                        if (WantCollect)
                        {
                            E.AddAction("Collect", "collect liquid", "CollectLiquid", Key: 'c', Default: CollectPriority);
                        }
                        if (UsableForCleaning() && CheckAnythingToCleanEvent.Check(CascadeFrom: ThePlayer, Using: ParentObject))
                        {
                            E.AddAction("CleanAll", "clean all your items [1 dram]", "CleanWithLiquid", Key: 'n', Default: 20);
                        }
                    }
                    else
                    if (AutoCollectLiquidType != null)
                    {
                        if (epistemicStatus == Examiner.EPISTEMIC_STATUS_KNOWN)
                        {
                            E.AddAction("AutoCollect", "stop auto-collecting liquid", "AutoCollectLiquid", Key: 'a', Default: -1);
                        }
                    }
                    else
                    if (ParentObject.InInventory == E.Actor || ParentObject.Equipped == E.Actor)
                    {
                        if (epistemicStatus == Examiner.EPISTEMIC_STATUS_KNOWN)
                        {
                            if (!GetPreferredLiquidEvent.GetFor(ParentObject, E.Actor).IsNullOrEmpty())
                            {
                                E.AddAction("AutoCollect", "auto-collect liquid", "AutoCollectLiquid", Key: 'a', Default: -1);
                            }
                        }
                    }
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetExtrinsicValueEvent E)
        {
            // we don't gate this behind GetEpistemicStatus() != Examiner.EPISTEMIC_STATUS_UNKNOWN
            // because that would let you buy strange artifacts from traders that contained valuable
            // liquids without paying for the value of the liquids; it's less janky to let you get
            // the value of liquids that are hidden from you in strange artifacts you're selling
            if (IsFreshWater())
            {
                E.Value += Volume;
            }
            else
            {
                var pure = IsPure();
                foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                {
                    float LiquidValue = Volume * KV.Value * Liquids[KV.Key].GetExtrinsicValuePerDram(pure) / 1000f;
                    E.Value += LiquidValue;
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetExtrinsicWeightEvent E)
        {
            E.Weight += GetLiquidWeight();
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(InventoryActionEvent E)
        {
            if (E.Command == "Drink")
            {
                if (!EffectivelySealed())
                {
                    CanDrinkEvent.GetFor(
                        Object: E.Actor,
                        Liquid: this,
                        CanDrinkThis: out bool canDrinkThis,
                        CouldDrinkOther: out bool couldDrinkOther
                    );
                    if (!canDrinkThis)
                    {
                        if (couldDrinkOther)
                        {
                            E.Actor.Fail("You are unable to drink that."); // todo:jason
                        }
                        else
                        {
                            E.Actor.Fail("You are unable to drink liquids."); // todo:jason
                        }
                        return false;
                    }
                    if (ParentObject.IsInStasis())
                    {
                        E.Actor.Fail("You cannot seem to interact with " + ParentObject.t() + " in any way.");
                        return false;
                    }
                    if (E.Actor.IsPlayer() && ConsiderLiquidDangerousToDrink() && Options.ConfirmDangerousLiquid)
                    {
                        if (Popup.WarnYesNoCancel("Are you sure you want to drink that?") != DialogResult.Yes)
                        {
                            return false;
                        }
                    }
                    if (ParentObject.IsTemporary)
                    {
                        PlayWorldSound("Sounds/Interact/sfx_interact_liquidContainer_drink");
                        if (E.Actor.IsPlayer())
                        {
                            AddPlayerMessage("It's fizzy.");
                        }
                        Empty();
                        return false;
                    }
                    else
                    if (ParentObject.FireEvent(Event.New("BeforeDrink", "Drinker", E.Actor), E))
                    {
                        int wasFreshWater = 0;
                        if (E.Actor.IsPlayer() && ParentObject.Owner != null)
                        {
                            if (Popup.ShowYesNoCancel(ParentObject.IndicativeDistal + ParentObject.Is + " not owned by you. Are you sure you want to drink from " + ParentObject.them + "?") != DialogResult.Yes)
                            {
                                return false;
                            }
                            else
                            {
                                ParentObject.Physics.BroadcastForHelp(E.Actor);
                            }
                        }

                        if (!E.Actor.FireEvent(Event.New("DrinkingFrom", "Container", ParentObject)))
                        {
                            return false;
                        }
                        bool drankSomething = false;
                        if (Volume > 0)
                        {
                            StringBuilder sMessage = Event.NewStringBuilder();
                            int startWater = 0;
                            int startHunger = 0;

                            Stomach pStomach = E.Actor.GetPart<Stomach>();
                            if (pStomach != null)
                            {
                                startWater = pStomach.Water;
                                startHunger = pStomach.HungerLevel;
                            }
                            List<string> keylist = getKeylist();
                            try
                            {
                                keylist.Clear();
                                keylist.AddRange(ComponentLiquids.Keys);
                                foreach (string b in keylist)
                                {
                                    bool result = Liquids[b].Drank(this, 1, E.Actor, sMessage, E);
                                    if ( !result )
                                    {
                                        if( sMessage.Length > 0 ) Popup.ShowBlock(sMessage.ToString());
                                        return false;
                                    }
                                }
                            }
                            finally
                            {
                                keylistPool.Enqueue(keylist);
                            }

                            if( IsFreshWater()) wasFreshWater = 1;

                            UseDram();
                            if (pStomach != null)
                            {
                                if (pStomach.Water != startWater || pStomach.HungerLevel != startHunger)
                                {
                                    sMessage.Compound("You are now ");
                                    if (pStomach.Water != startWater)
                                    {
                                        sMessage.Append(pStomach.WaterStatus());
                                        if (pStomach.HungerLevel != startHunger)
                                        {
                                            sMessage.Append(" and ");
                                        }
                                    }
                                    if (pStomach.HungerLevel != startHunger)
                                    {
                                        sMessage.Append(pStomach.FoodStatus());
                                    }
                                    sMessage.Append('.');
                                }
                            }

                            PlayWorldSound("sfx_interact_liquidContainer_drink");
                            if (E.Actor.IsPlayer() && sMessage.Length > 0)
                            {
                                Popup.ShowBlock(sMessage.ToString());
                            }
                            if (Volume <= 0)
                            {
                                Empty(WillCheckImage: true);
                            }
                            drankSomething = true;
                        }
                        else
                        {
                            E.Actor.Fail("It's empty, there's nothing left to drink!");
                            Empty(WillCheckImage: true);
                        }
                        CheckImage();
                        if (!drankSomething)
                        {
                            return false;
                        }
                        if (E.Actor != null)
                        {
                            if (E.Actor.HasRegisteredEvent("Drank"))
                            {
                                E.Actor.FireEvent(Event.New("Drank", "Object", ParentObject, "WasFreshWater", wasFreshWater ));
                            }
                            E.Actor.UseEnergy(1000, "Item Drink");
                            E.RequestInterfaceExit();
                        }
                    }
                }
            }
            else
            if (E.Command == "Pour")
            {
                Pour(
                    Actor: E.Actor,
                    RequestInterfaceExit: ref E.InterfaceExit
                );
            }
            else
            if (E.Command == "Fill")
            {
                PerformFill(
                    Actor: E.Actor,
                    RequestInterfaceExit: ref E.InterfaceExit,
                    ownershipHandled: E.OwnershipHandled
                );
            }
            else
            if (E.Command == "Drain")
            {
                if (!HasDrain)
                {
                    E.Actor.Fail(ParentObject.Does("have") + " no drain.");
                    return false;
                }
                if (EffectivelySealed())
                {
                    E.Actor.Fail(ParentObject.Does("are") + " sealed.");
                    return false;
                }
                if (ParentObject.IsInStasis())
                {
                    E.Actor.Fail("You cannot seem to interact with " + ParentObject.t() + " in any way.");
                    return false;
                }
                if (Volume <= 0)
                {
                    E.Actor.Fail(ParentObject.Does("are") + " empty.");
                    return false;
                }
                if (!E.Actor.CheckFrozen(Telekinetic: true))
                {
                    return false;
                }
                if (E.Actor.IsPlayer())
                {
                    if (ParentObject.Owner != null && !E.OwnershipHandled)
                    {
                        if (Popup.ShowYesNoCancel(ParentObject.IndicativeDistal + ParentObject.Is + " not owned by you. Are you sure you want to drain " + ParentObject.them + "?") != DialogResult.Yes)
                        {
                            return false;
                        }
                        else
                        {
                            ParentObject.Physics.BroadcastForHelp(E.Actor);
                        }
                    }
                    else
                    {
                        if (Popup.ShowYesNoCancel("Are you sure you want to drain " + ParentObject.t() + "?") != DialogResult.Yes)
                        {
                            return false;
                        }
                    }
                }
                EmptyIntoCell(Pourer: E.Actor);
            }
            else
            if (E.Command == "FillFrom")
            {
                if (EffectivelySealed())
                {
                    E.Actor.Fail(ParentObject.Does("are") + " sealed.");
                    return false;
                }
                if (ParentObject.IsInStasis())
                {
                    E.Actor.Fail("You cannot seem to interact with " + ParentObject.t() + " in any way.");
                    return false;
                }
                if (!E.Actor.CheckFrozen(Telekinetic: true))
                {
                    return false;
                }
                if (E.Actor.IsPlayer() && ParentObject.Owner != null && !E.OwnershipHandled)
                {
                    if (Popup.ShowYesNoCancel(ParentObject.IndicativeDistal + ParentObject.Is + " not owned by you. Are you sure you want to fill " + ParentObject.them + "?") != DialogResult.Yes)
                    {
                        return false;
                    }
                    else
                    {
                        ParentObject.Physics.BroadcastForHelp(E.Actor);
                    }
                }
                List<GameObject> sameLiquid = Event.NewGameObjectList();
                List<GameObject> otherLiquid = Event.NewGameObjectList();
                List<GameObject> Containers = Event.NewGameObjectList();
                List<GameObject> inventory = Event.NewGameObjectList();
                E.Actor.GetInventoryAndEquipment(inventory);
                for (int i = 0, j = inventory.Count; i < j; i++)
                {
                    GameObject obj = inventory[i];
                    if (obj != ParentObject)
                    {
                        LiquidVolume LV = obj.LiquidVolume;
                        if (LV != null && !LV.EffectivelySealed() && LV.Volume > 0)
                        {
                            if (LV.LiquidSameAs(this))
                            {
                                sameLiquid.Add(obj);
                            }
                            else
                            {
                                otherLiquid.Add(obj);
                            }
                        }
                    }
                }
                Containers.AddRange(sameLiquid);
                Containers.AddRange(otherLiquid);
                if (Containers.Count == 0)
                {
                    E.Actor.Fail("You have no containers to pour from.");
                    return true;
                }

                GameObject Container = PickItem.ShowPicker(
                    Items: Containers,
                    Actor: E.Actor,
                    PreserveOrder: true,
                    Title: "[Select a container to fill from]"
                );
                if (Container != null)
                {
                    try
                    {
                        ParentObject.SplitFromStack();
                        if (Container == ParentObject)
                        {
                            E.Actor.Fail("You can't pour from a container into " + ParentObject.itself + ".");
                            return false;
                        }
                        if (Container.IsTemporary)
                        {
                            E.Actor.Fail("It's fizzy.");
                            return false;
                        }

                        LiquidVolume pContainer = Container.LiquidVolume;
                        bool EmptyContainer = false;

                        if (Volume > 0 && !pContainer.LiquidSameAs(this))
                        {
                            DialogResult Result = Popup.ShowYesNoCancel("Do you want to empty " + ParentObject.t() + " first?");

                            if (Result == DialogResult.Cancel)
                            {
                                return true;
                            }
                            if (Result == DialogResult.Yes)
                            {
                                EmptyContainer = true;
                            }
                        }

                        int PourableVolume = Math.Min(EmptyContainer ? MaxVolume : MaxVolume - Volume, pContainer.Volume);

                        int? PourAmountSpec = UI.Popup.AskNumber("How many drams? (max=" + PourableVolume + ")", Start: PourableVolume, Min: 0, Max: PourableVolume);
                        int PourAmount = 0;

                        try
                        {
                            PourAmount = Convert.ToInt32(PourAmountSpec);
                        }
                        catch
                        {
                            return true;
                        }

                        if (PourAmount > pContainer.Volume)
                        {
                            PourAmount = pContainer.Volume;
                        }
                        if (PourAmount <= 0)
                        {
                            return true;
                        }

                        if (EmptyContainer)
                        {
                            EmptyIntoCell(Pourer: E.Actor);
                        }

                        int MaxFillAmount = MaxVolume - Volume;

                        int FinalFillAmount = 0;

                        if (MaxFillAmount < pContainer.Volume)
                        {
                            FinalFillAmount = MaxFillAmount;
                        }
                        else
                        {
                            FinalFillAmount = pContainer.Volume;
                        }
                        if (FinalFillAmount > PourAmount)
                        {
                            FinalFillAmount = PourAmount;
                        }

                        MixWith(
                            Liquid: pContainer,
                            Amount: FinalFillAmount,
                            PouredFrom: Container,
                            PouredBy: E.Actor,
                            RequestInterfaceExit: ref E.InterfaceExit
                        );
                        CheckImage();
                        pContainer.CheckImage();
                    }
                    finally
                    {
                        ParentObject.CheckStack();
                    }
                }
            }
            else
            if (E.Command == "CollectLiquid")
            {
                if (Volume <= 0 || EffectivelySealed())
                {
                    return false;
                }
                if (!E.Actor.CanMoveExtremities(AllowTelekinetic: true, ShowMessage: true))
                {
                    return false;
                }
                if (ParentObject.IsInStasis())
                {
                    E.Actor.Fail("You cannot seem to interact with " + ParentObject.t() + " in any way.");
                    return false;
                }
                if (ParentObject.IsTemporary)
                {
                    E.Actor.Fail("It's fizzy.");
                    Empty();
                    return false;
                }
                string liquid = GetLiquidDesignation();
                int storable =
                    E.Auto
                    ? E.Actor.GetAutoCollectDrams(liquid, skip: ParentObject)
                    : E.Actor.GetStorableDrams(liquid, skip: ParentObject)
                ;
                if (storable <= 0)
                {
                    if (!E.Auto)
                    {
                        E.Actor.Fail("You have nowhere available to collect that.");
                    }
                }
                else
                {
                    if (E.Actor.IsPlayer() && ParentObject.Owner != null && !E.OwnershipHandled)
                    {
                        if (E.Auto)
                        {
                            goto CollectDone;
                        }
                        if (Popup.ShowYesNoCancel(ParentObject.IndicativeDistal + ParentObject.Is + " not owned by you. Are you sure you want to collect from " + ParentObject.them + "?") != DialogResult.Yes)
                        {
                            goto CollectDone;
                        }
                        else
                        {
                            ParentObject.Physics.BroadcastForHelp(E.Actor);
                        }
                    }
                    int transfer = Math.Min(storable, Volume);
                    if (E.Actor.IsPlayer() && !E.Auto && transfer > 128)
                    {
                        if (Popup.ShowYesNoCancel("You are able to collect " + transfer.Things("dram") + " of " + GetLiquidName() + ". Are you sure you want to?") != DialogResult.Yes)
                        {
                            goto CollectDone;
                        }
                    }
                    List<GameObject> containers = Event.NewGameObjectList();
                    E.Actor.GiveDrams(transfer, liquid, skip: ParentObject, auto: E.Auto, storedIn: containers, liquidVolume: this);
                    StringBuilder msg = Event.NewStringBuilder();
                    msg
                        .Append(E.Actor.Does("collect"))
                        .Append(' ')
                        .Append(transfer.Things("dram"))
                        .Append(" of ")
                        .Append(GetLiquidName())
                    ;
                    if (IsOpenVolume())
                    {
                        msg
                            .Append(' ')
                            .Append(E.Actor.DescribeDirectionFrom(ParentObject))
                        ;
                    }
                    else
                    {
                        msg
                            .Append(" from ")
                            .Append(ParentObject.t())
                            .Append(' ')
                            .Append(E.Actor.DescribeDirectionToward(ParentObject))
                        ;
                    }
                    if (containers.Count > 0)
                    {
                        List<string> regularNames = new();
                        List<bool> regularPlurals = new();
                        List<string> properNames = new();
                        Dictionary<string, int> regularCounts = new();
                        foreach (GameObject obj in containers)
                        {
                            string name = obj.DisplayNameOnly;
                            if (obj.HasProperName)
                            {
                                properNames.Add(name);
                            }
                            else
                            {
                                if (regularNames.Contains(name))
                                {
                                    regularCounts[name]++;
                                }
                                else
                                {
                                    regularNames.Add(name);
                                    regularPlurals.Add(obj.IsPlural);
                                    regularCounts.Add(name, 1);
                                }
                            }
                        }
                        msg.Append(" in ");
                        if (properNames.Count > 0)
                        {
                            msg.Append(Grammar.MakeAndList(properNames));
                            if (regularNames.Count > 0)
                            {
                                msg.Append(" and ");
                            }
                        }
                        if (regularNames.Count > 0)
                        {
                            for (int i = 0, j = regularNames.Count; i < j; i++)
                            {
                                if (!regularPlurals[i] && regularCounts[regularNames[i]] > 1)
                                {
                                    regularNames[i] = Grammar.Pluralize(regularNames[i]);
                                }
                            }
                            msg
                                .Append(E.Actor.its)
                                .Append(' ')
                                .Append(Grammar.MakeAndList(regularNames))
                            ;
                        }
                    }
                    msg.Append('.');
                    EmitMessage(E.Actor, msg, FromDialog: !E.Auto);
                    Volume -= transfer;
                    Update();
                    E.Actor.UseEnergy(1000, "Item Liquid Collect");
                }
                CollectDone:;
            }
            else
            if (E.Command == "CleanWithLiquid")
            {
                bool doClean = true;
                if (ParentObject.IsInStasis())
                {
                    E.Actor.Fail("You cannot seem to interact with " + ParentObject.t() + " in any way.");
                    return false;
                }
                if (!E.Actor.CanMoveExtremities(AllowTelekinetic: true, ShowMessage: true))
                {
                    return false;
                }
                if (E.Actor.IsPlayer() && ParentObject.Owner != null && !E.OwnershipHandled)
                {
                    if (Popup.ShowYesNoCancel(ParentObject.IndicativeDistal + ParentObject.Is + " not owned by you. Are you sure you want to use " + GetLiquidName() + " from " + ParentObject.them + "?") != DialogResult.Yes)
                    {
                        doClean = false;
                    }
                    else
                    {
                        ParentObject.Physics.BroadcastForHelp(E.Actor);
                    }
                }
                if (doClean)
                {
                    List<GameObject> objs = null;
                    List<string> types = null;
                    if (UsableForCleaning())
                    {
                        CleanItemsEvent.PerformFor(E.Actor, E.Actor, ParentObject, out objs, out types);
                    }
                    if (objs != null && objs.Count > 0)
                    {
                        CleaningMessage(
                            Actor: E.Actor,
                            Objects: objs,
                            Types: types,
                            Source: ParentObject,
                            LiquidVolume: this,
                            UseDram: true
                        );
                        E.Actor.UseEnergy(1000, "Cleaning");
                        E.RequestInterfaceExit();
                    }
                    else
                    {
                        E.Actor.Fail("You cannot do that for some reason.");
                    }
                }
            }
            else
            if (E.Command == "AutoCollectLiquid")
            {
                if (AutoCollectLiquidType != null)
                {
                    AutoCollectLiquidType = null;
                }
                else
                {
                    if (Sealed || ComponentLiquids.Count > 1)
                    {
                        E.Actor.Fail("Auto collection only works on unsealed containers with pure liquids.");
                    }
                    else
                    if (ComponentLiquids.Count == 0)
                    {
                        AutoCollectLiquidType = GetPreferredLiquidEvent.GetFor(ParentObject, E.Actor);
                        if (AutoCollectLiquidType == null)
                        {
                            E.Actor.Fail("It isn't clear what kind of liquid would be appropriate for " + ParentObject.t() + " to collect. Pour a pure liquid into it, and then enable auto-collect.");
                        }
                    }
                    else
                    {
                        foreach (string ID in ComponentLiquids.Keys)
                        {
                            AutoCollectLiquidType = ID;
                            break;
                        }
                    }
                }
            }
            else
            if (E.Command == "Seal")
            {
                if (!Sealed)
                {
                    if (!E.Actor.CheckFrozen(Telekinetic: true))
                    {
                        return false;
                    }
                    if (ParentObject.IsInStasis())
                    {
                        E.Actor.Fail("You cannot seem to interact with " + ParentObject.t() + " in any way.");
                        return false;
                    }
                    Sealed = true;
                    PlayWorldSound("Sounds/Interact/sfx_interact_liquidContainer_seal");
                    if (!EffectivelySealed())
                    {
                        XDidYToZ(E.Actor, "seal", ParentObject, "as best " + E.Actor.it + " can, but in " + ParentObject.its + " condition this does little", FromDialog: true);
                    }
                    else
                    {
                        XDidYToZ(E.Actor, "seal", ParentObject, FromDialog: true);
                    }
                    E.Actor.UseEnergy(1000, "Item Seal");
                }
            }
            else
            if (E.Command == "Unseal")
            {
                if (Sealed)
                {
                    if (!E.Actor.CheckFrozen(Telekinetic: true))
                    {
                        return false;
                    }
                    if (ParentObject.IsInStasis())
                    {
                        E.Actor.Fail("You cannot seem to interact with " + ParentObject.t() + " in any way.");
                        return false;
                    }
                    PlayWorldSound("Sounds/Interact/sfx_interact_liquidContainer_unseal");
                    Sealed = false;
                    XDidYToZ(E.Actor, "unseal", ParentObject, FromDialog: true);
                    E.Actor.UseEnergy(1000, "Item Unseal");
                }
            }
            return base.HandleEvent(E);
        }

        private static string GetNameForCleaningMessage(GameObject Object)
        {
            return Object.GetDisplayName(
                Short: true,
                AsPossessed: true,
                WithIndefiniteArticle: true,
                SecondPerson: true
            ) ?? "something";
        }

        public static void CleaningMessage(
            GameObject Actor,
            List<GameObject> Objects,
            List<string> Types = null,
            GameObject Source = null,
            LiquidVolume LiquidVolume = null,
            bool UseDram = false
        )
        {
            List<string> items = new List<string>(Objects.Count);
            if (Objects.Contains(Actor))
            {
                items.Add(Actor.itself);
            }
            foreach (GameObject obj in Objects)
            {
                if (obj != Actor && obj.HasProperName)
                {
                    items.Add(GetNameForCleaningMessage(obj));
                }
            }
            foreach (GameObject obj in Objects)
            {
                if (obj != Actor && !obj.HasProperName && obj.Equipped == Actor)
                {
                    items.Add(GetNameForCleaningMessage(obj));
                }
            }
            foreach (GameObject obj in Objects)
            {
                if (obj != Actor && !obj.HasProperName && obj.Equipped != Actor)
                {
                    items.Add(GetNameForCleaningMessage(obj));
                }
            }
            foreach (GameObject obj in Objects)
            {
                obj.CheckStack();
            }
            StringBuilder sb = Event.NewStringBuilder();
            sb.Append("the ");
            if (Types.IsNullOrEmpty())
            {
                sb.Append("mess");
            }
            else
            {
                sb.Append(Grammar.MakeAndList(Types));
            }
            sb
                .Append(" from ")
                .Append(Grammar.MakeAndList(items))
            ;
            LiquidVolume ??= Source?.LiquidVolume;
            if (LiquidVolume != null)
            {
                sb.Append(" with a dram of ");
                LiquidVolume.AppendLiquidName(sb);
                if (Source != null)
                {
                    if (LiquidVolume.IsOpenVolume())
                    {
                        if (Source.CurrentCell != null)
                        {
                            sb
                                .Append(' ')
                                .Append(Actor.DescribeDirectionFrom(Source))
                            ;
                        }
                    }
                    else
                    {
                        sb
                            .Append(" from ")
                            .Append(Source.GetDisplayName(Short: true, AsPossessed: true, WithDefiniteArticle: true))
                        ;
                        if (Source.CurrentCell != null)
                        {
                            sb
                                .Append(' ')
                                .Append(Actor.DescribeDirectionToward(Source))
                            ;
                        }
                    }
                }
            }
            else
            if (Source != null)
            {
                sb
                    .Append(" with ")
                    .Append(GetNameForCleaningMessage(Source))
                ;
            }
            XDidY(Actor, "clean", sb.ToString());
            if (UseDram && LiquidVolume != null)
            {
                LiquidVolume.FlowIntoCell(Amount: 1, TargetCell: Actor.GetCurrentCell(), Pourer: Actor);
            }
        }

        public override bool HandleEvent(CanSmartUseEvent E)
        {
            if (IsOpenVolume() || (!ParentObject.IsCreature && !EffectivelySealed()) )
            {
                if( !IsFreshWater() && E.MinPriority > 0 ) return true;
                return false; // positive case
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(CommandSmartUseEvent E)
        {
            if (IsOpenVolume() || (!ParentObject.IsCreature && !EffectivelySealed()))
            {
                ParentObject.Twiddle();
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(ObjectCreatedEvent E)
        {
            if (!StartVolume.IsNullOrEmpty())
            {
                Volume = StartVolume.RollCached();
            }
            if (Volume == 0)
            {
                Empty(NoDestroy: true);
            }
            else
            {
                CheckImage();
            }
            SyncPhysicalProperties();
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(EffectAppliedEvent E)
        {
            CheckImage();
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(EffectRemovedEvent E)
        {
            CheckImage();
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(StripContentsEvent E)
        {
            if (!IsEmpty())
            {
                if (
                    !E.KeepNatural
                    || ParentObject == null
                    || GetLiquidDesignation( hyphenateSoloLiquids:true ) != ParentObject.GetBlueprint().GetPartParameter<string>("LiquidVolume", "InitialLiquid")
                )
                {
                    Empty();
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(OnDestroyObjectEvent E)
        {
            if (!IsOpenVolume() && !E.Obliterate)
            {
                MaxVolume = -1;
                GameObject actor = ParentObject.Holder;
                Cell actorCell = actor?.GetCellContext();
                Cell targetCell = ParentObject.GetCellContext() ?? ParentObject.GetObjectContext()?.GetCellContext() ?? actorCell;
                if (targetCell != null)
                {
                    Pour(
                        Actor: actor,
                        TargetCell: targetCell,
                        Douse: (actorCell == targetCell),
                        Forced: true,
                        RequestInterfaceExit: ref E.InterfaceExit
                    );
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(FellDownEvent E)
        {
            if (IsOpenVolume())
            {
                EmptyIntoCell(E.Cell);
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(VaporizedEvent E)
        {
            if (Volume > 0)
            {
                List<string> keylist = getKeylist();
                try
                {
                    keylist.Clear();
                    keylist.AddRange(ComponentLiquids.Keys);
                    foreach (string liquid in keylist)
                    {
                        if (!Liquids[liquid].Vaporized(this, E.By))
                        {
                            return false;
                        }
                    }
                }
                finally
                {
                    keylistPool.Enqueue(keylist);
                    CheckImage();
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(FrozeEvent E)
        {
            if (Volume > 0)
            {
                List<string> keylist = getKeylist();
                try
                {
                    keylist.Clear();
                    keylist.AddRange(ComponentLiquids.Keys);
                    foreach (string liquid in keylist)
                    {
                        if (Liquids.ContainsKey(liquid) && !Liquids[liquid].Froze(this, E.By))
                        {
                            return false;
                        }
                    }
                }
                finally
                {
                    keylistPool.Enqueue(keylist);
                    CheckImage();
                }
                Cell C = ParentObject.CurrentCell;
                string freezeObject = GetLiquidFreezeObject(out string freezeVerb);
                if (!string.IsNullOrEmpty(freezeObject) && ParentObject.IsValid() )
                {
                    int gasDensity = Volume * ParentObject.Count / 20;
                    PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_frozen");
                    ParentObject.Physics.LastDamagedByType = "Frozen";
                    ParentObject.Physics.LastDamageAccidental = true;
                    ParentObject.Die(
                        Killer: E.By,
                        Reason: "You froze.",
                        ThirdPersonReason: ParentObject.It + " @@froze.",
                        Accidental: true,
                        Message: (freezeVerb.IsNullOrEmpty() ? "" : null),
                        DeathVerb: (freezeVerb.IsNullOrEmpty() ? null : freezeVerb)
                    );
                    if (C != null)
                    {
                        GameObject obj = GameObject.Create(freezeObject, Context: "Freeze");

                        if( obj == null )
                        {
                            MetricsManager.LogError("Unknown freeze object " + freezeObject);
                            return true;
                        }

                        Gas pGas = obj.GetPart<Gas>();
                        if (pGas != null)
                        {
                            pGas.Density = gasDensity;
                            if (GameObject.Validate(E.By))
                            {
                                pGas.Creator = E.By;
                            }
                        }
                        C.AddObject(obj);
                        for (int i = 0, j = C.Objects.Count; i < j; i++)
                        {
                            GameObject other = C.Objects[i];
                            if (
                                other != null
                                && other != obj
                                && other.IsCombatObject(NoBrainOnly: true)
                                && obj.ConsiderSolidFor(other)
                            )
                            {
                                var fx = new Effects.Stuck(
                                    Duration: 30,
                                    SaveTarget: 25,
                                    SaveVs: "Frozen Stuck Restraint",
                                    DependsOn: obj.ID,
                                    DependsOnMustBeSolid: true
                                );
                                if (other.ApplyEffect(fx))
                                {
                                    other.RemoveAllEffects<LiquidCovered>();
                                    foreach (GameObject item in other.GetInventoryAndEquipment())
                                    {
                                        item.RemoveAllEffects<LiquidCovered>();
                                    }
                                }
                            }
                        }
                    }
                    return false;
                }
                if (CanInteractWithAnything(C) && ParentObject.CurrentCell != null )
                {
                    foreach (GameObject GO in ParentObject.CurrentCell.GetObjectsWithPartReadonly("Combat"))
                    {
                        if (CanInteractWith(GO, C, AnythingChecked: true) && Volume >= GO.GetMaximumLiquidExposure() / 8)
                        {
                            var fx = new Effects.Stuck(
                                Duration: 5,
                                SaveTarget: 15,
                                SaveVs: "Frozen Stuck Restraint",
                                DependsOn: ParentObject.ID,
                                DependsOnMustBeFrozen: true
                            );
                            GO.ApplyEffect(fx);
                        }
                    }
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(ThawedEvent E)
        {
            if (Volume > 0)
            {
                List<string> keylist = getKeylist();
                try
                {
                    keylist.Clear();
                    keylist.AddRange(ComponentLiquids.Keys);
                    foreach (string liquid in keylist)
                    {
                        if (!Liquids[liquid].Thawed(this, E.By))
                        {
                            return false;
                        }
                    }
                }
                finally
                {
                    keylistPool.Enqueue(keylist);
                    CheckImage();
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(PollForHealingLocationEvent E)
        {
            if (Volume > 0)
            {
                List<string> keylist = getKeylist();
                try
                {
                    keylist.Clear();
                    keylist.AddRange(ComponentLiquids.Keys);
                    foreach (string b in keylist)
                    {
                        int liquidValue = Liquids[b].GetHealingLocationValue(this, E.Actor);
                        if (liquidValue > E.Value)
                        {
                            E.Value = liquidValue;
                            if (E.First)
                            {
                                return false;
                            }
                        }
                    }
                }
                finally
                {
                    keylistPool.Enqueue(keylist);
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(InterruptAutowalkEvent E)
        {
            if (
                IsSwimmingDepth()
                && Options.ConfirmSwimming
                && !E.Actor.HasEffect<Effects.Swimming>()
                && CanInteractWith(E.Actor, E.Cell)
            )
            {
                E.IndicateObject = ParentObject;
                return false;
            }
            if (Options.ConfirmDangerousLiquid)
            {
                foreach (string LiquidID in ComponentLiquids.Keys)
                {
                    if (Liquids[LiquidID].InterruptAutowalk)
                    {
                        if (CanInteractWith(E.Actor, E.Cell) && !EffectivelySealed() && IsOpenVolume() )
                        {
                            E.IndicateObject = ParentObject;
                            return false;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(CheckAnythingToCleanWithEvent E)
        {
            if (UsableForCleaning())
            {
                return false; // positive case
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(CheckAnythingToCleanWithNearbyEvent E)
        {
            if (UsableForCleaning())
            {
                return false; // positive case
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetCleaningItemsEvent E)
        {
            if (UsableForCleaning())
            {
                E.Objects.Add(ParentObject);
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetCleaningItemsNearbyEvent E)
        {
            if (UsableForCleaning())
            {
                E.Objects.Add(ParentObject);
            }
            return base.HandleEvent(E);
        }

        public override bool WantTurnTick()
        {
            return true;
        }

        public override void TurnTick(long TimeTick, int Amount)
        {
            ProcessTurns(Amount);
        }

        public void ProcessTurns(int Turns)
        {
            Cell ourCell = ParentObject.CurrentCell;
            if (ourCell != null && ourCell.ParentZone != null && ourCell.ParentZone.IsActive())
            {
                if (Primary == LiquidWax.ID && IsOpenVolume()) // if the primary is wax
                {
                    //if( ParentObject.pPhysics.Temperature <= ComponentLiquidNameMap["wax"].FlameTemperature )
                    // THIS DOESN'T WORK; DEBUG WHEN YOU GET A CHANCE AND REPLACE THE NEXT LINE
                    if (ParentObject.Physics.Temperature < 25)
                    {
                        if (!ourCell.IsOccluding())
                        {
                            if (Volume >= 200)
                            {
                                ourCell.AddObject("Wax Block");
                            }
                            else
                            {
                                ourCell.AddObject("Wax Nodule");
                            }
                            ParentObject.Destroy(Silent: true);
                        }
                    }
                }
                List<string> keylist = null;
                try
                {
                    if (ComponentLiquids.Count == 1 && Primary != null)
                    {
                        RequirePrimaryLiquid().ProcessTurns(this, ParentObject, Turns);
                    }
                    else
                    if (ComponentLiquids.Count == 2 && Primary != null && Secondary != null)
                    {
                        RequirePrimaryLiquid().ProcessTurns(this, ParentObject, Turns);
                        if (Secondary != null)
                        {
                            RequireSecondaryLiquid().ProcessTurns(this, ParentObject, Turns);
                        }
                    }
                    else
                    {
                        keylist = getKeylist();
                        keylist.Clear();
                        keylist.AddRange(ComponentLiquids.Keys);
                        foreach (string liquid in keylist)
                        {
                            Liquids[liquid].ProcessTurns(this, ParentObject, Turns);
                        }
                    }
                    if (Volume > 0 && CanInteractWithAnything(ourCell))
                    {
                        for (int i = 0, j = ourCell.Objects.Count; i < j; i++)
                        {
                            GameObject GO = ourCell.Objects[i];
                            if (CanInteractWith(GO, ourCell, AnythingChecked: true))
                            {
                                if (ComponentLiquids.Count == 1 && Primary != null)
                                {
                                    RequirePrimaryLiquid().ObjectInCell(this, GO);
                                }
                                else
                                if (ComponentLiquids.Count == 2 && Primary != null && Secondary != null)
                                {
                                    RequirePrimaryLiquid().ObjectInCell(this, GO);
                                    if (Secondary != null)
                                    {
                                        RequireSecondaryLiquid().ObjectInCell(this, GO);
                                    }
                                }
                                else
                                {
                                    keylist ??= getKeylist();
                                    foreach (string b in keylist)
                                    {
                                        if (ComponentLiquids.ContainsKey(b))
                                        {
                                            Liquids[b].ObjectInCell(this, GO);
                                        }
                                    }
                                }
                                if (IsOpenVolume())
                                {
                                    ProcessContact(GO, Prone: GO.HasEffect<Effects.Prone>(), FromCell: true);
                                }
                            }
                            if (j != ourCell.Objects.Count)
                            {
                                j = ourCell.Objects.Count;
                                if (i < j && ourCell.Objects[i] != GO)
                                {
                                    i--;
                                }
                            }
                        }
                    }
                    if (Volume > 0 && CanEvaporate())
                    {
                        int evaporativity = GetLiquidEvaporativity();
                        if (evaporativity > 0)
                        {
                            int toEvaporate = 0;
                            for (int i = 0; i < Volume; i++)
                            {
                                if (evaporativity.in100())
                                {
                                    toEvaporate++;
                                }
                            }
                            if (toEvaporate > 0)
                            {
                                UseDramsByEvaporativity(toEvaporate);
                            }
                        }
                    }
                }
                finally
                {
                    if (keylist != null)
                    {
                        keylistPool.Enqueue(keylist);
                    }
                }
            }
            else
            if (ParentObject != null)
            {
                if (ComponentLiquids.Count == 1 && Primary != null)
                {
                    RequirePrimaryLiquid().ProcessTurns(this, ParentObject, Turns);
                }
                else
                if (ComponentLiquids.Count == 2 && Primary != null && Secondary != null)
                {
                    RequirePrimaryLiquid().ProcessTurns(this, ParentObject, Turns);
                    if (Secondary != null)
                    {
                        RequireSecondaryLiquid().ProcessTurns(this, ParentObject, Turns);
                    }
                }
                else
                {
                    List<string> keylist = getKeylist();
                    try
                    {
                        keylist.Clear();
                        keylist.AddRange(ComponentLiquids.Keys);
                        foreach (string liquid in keylist)
                        {
                            Liquids[liquid].ProcessTurns(this, ParentObject, Turns);
                        }
                    }
                    finally
                    {
                        keylistPool.Enqueue(keylist);
                    }
                }
            }
        }

        public void RenderSmear(RenderEvent E, GameObject obj)
        {
            if (Primary != null || Secondary != null)
            {
                string native = obj?.GetPropertyOrTag("LiquidNative");
                if (Primary != null && native != Primary)
                {
                    RequirePrimaryLiquid().RenderSmearPrimary(this, E, obj);
                }
                if (Secondary != null && native != Secondary)
                {
                    RequireSecondaryLiquid().RenderSmearSecondary(this, E, obj);
                }
            }
        }

        public void RenderStain(RenderEvent E, GameObject obj)
        {
            if (Primary != null)
            {
                string Color = RequirePrimaryLiquid().GetColor();
                if (Color != null)
                {
                    E.ColorString = "&" + Color;
                }
            }
            if (Secondary != null)
            {
                string Color = RequireSecondaryLiquid().GetColor();
                if (Color != null)
                {
                    E.DetailColor = Color;
                }
            }
        }

        public void Empty(bool WillCheckImage = false, bool NoDestroy = false)
        {
            if (!NoDestroy && IsOpenVolume() && ParentObject?.CurrentCell != null)
            {
                ParentObject.Destroy(Silent: true, Obliterate: true);
            }
            else
            {
                if (Volume != 0)
                {
                    Volume = 0;
                    FlushWeightCaches();
                }
                ComponentLiquids.Clear();
                if (!WillCheckImage)
                {
                    CheckImage();
                }
            }
        }

        public void EmptyIntoCell(Cell C = null, GameObject Pourer = null)
        {
            PourIntoCell(Pourer, C ?? ParentObject.GetCurrentCell(), Volume, CanPourOn: true);
            Empty();
        }

        public int GetLiquidCount()
        {
            return ComponentLiquids.Count;
        }

        public string GetLiquidName()
        {
            if (ComponentLiquids.Count == 1)
            {
                if (Primary != null)
                {
                    return RequirePrimaryLiquid().GetName(this);
                }
                foreach (string b in ComponentLiquids.Keys)
                {
                    return Liquids[b].GetName(this);
                }
            }
            else
            if (ComponentLiquids.Count > 1)
            {
                StringBuilder SB = Event.NewStringBuilder();
                int Max = 0;
                string maxID = null;
                foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                {
                    if (KV.Value > Max || (KV.Value == Max && KV.Key.CompareTo(maxID) < 0))
                    {
                        maxID = KV.Key;
                        Max = KV.Value;
                    }
                }
                foreach (string LiquidID in ComponentLiquids.Keys)
                {
                    if (LiquidID != maxID)
                    {
                        SB.Append(Liquids[LiquidID].GetAdjective(this)).Append(' ');
                    }
                }
                SB.Append(Liquids[maxID].GetName(this));
                return SB.ToString();
            }
            return "";
        }

        private int CompareProportions(string a, string b)
        {
            return -ComponentLiquids[a].CompareTo(ComponentLiquids[b]);
        }

        public List<string> GetTertiaries()
        {
            int num = ComponentLiquids.Count;
            if (Primary != null)
            {
                num--;
            }
            if (Secondary != null)
            {
                num--;
            }
            if (num <= 0)
            {
                return null;
            }
            List<string> result = new List<string>(num);
            foreach (string Liquid in ComponentLiquids.Keys)
            {
                if (Liquid != Primary && Liquid != Secondary)
                {
                    result.Add(Liquid);
                }
            }
            if (num > 1)
            {
                result.Sort(CompareProportions);
            }
            return result;
        }

        public void AppendLiquidName(
            StringBuilder SB,
            bool Stripped = false
        )
        {
            if (ComponentLiquids.Count == 0)
            {
                return;
            }
            if (ComponentLiquids.Count == 1)
            {
                if (Primary != null)
                {
                    SB.Append(Liquids[Primary].GetName(this));
                }
                else
                {
                    foreach (string b in ComponentLiquids.Keys)
                    {
                        SB.Append(Liquids[b].GetName(this));
                    }
                }
            }
            else
            {
                bool any = false;
                if (Secondary != null)
                {
                    string adj = RequireSecondaryLiquid().GetAdjective(this);
                    if (!adj.IsNullOrEmpty())
                    {
                        SB.Append(adj);
                        any = true;
                    }
                }
                List<string> Tertiaries = GetTertiaries();
                if (Tertiaries != null)
                {
                    foreach (string Tertiary in Tertiaries)
                    {
                        string adj = Liquids[Tertiary].GetAdjective(this);
                        if (!adj.IsNullOrEmpty())
                        {
                            if (any)
                            {
                                SB.Append(' ');
                            }
                            else
                            {
                                any = true;
                            }
                            SB.Append(adj);
                        }
                    }
                }
                if (Primary != null)
                {
                    string name = RequirePrimaryLiquid().GetName(this);
                    if (!name.IsNullOrEmpty())
                    {
                        if (any)
                        {
                            SB.Append(' ');
                        }
                        SB.Append(name);
                    }
                }
            }
            if (Stripped)
            {
                ColorUtility.StripFormatting(SB);
            }
        }

        public string GetDescriptionPreposition()
        {
            return (ParentObject == null || ParentObject.Understood()) ? NamePreposition : UnknownNamePreposition;
        }

        public bool UsesNamePreposition()
        {
            return !GetDescriptionPreposition().IsNullOrEmpty();
        }

        public void AppendLiquidDescription(
            StringBuilder SB,
            bool IncludeAmount = true,
            bool IgnoreSeal = false,
            bool Stripped = false,
            bool Syntactic = false,
            bool ForSort = false,
            string UsePreposition = null
        )
        {
            UsePreposition ??= GetDescriptionPreposition();
            bool useSealed = !IgnoreSeal && EffectivelySealed();
            if (!useSealed || LiquidVisibleWhenSealed || ShowSeal)
            {
                if (!Syntactic && UsePreposition.IsNullOrEmpty())
                {
                    SB.Append(Stripped ? "[" : "{{y|[");
                }
                else
                if ((!useSealed || LiquidVisibleWhenSealed) && !UsePreposition.IsNullOrEmpty())
                {
                    SB.Append(UsePreposition).Append(' ');
                }
                if (!useSealed || LiquidVisibleWhenSealed)
                {
                    if (IncludeAmount)
                    {
                        if (ForSort)
                        {
                            if (Stripped)
                            {
                                SB
                                    .Append(Volume.ToString("D6"))
                                    .Append(Volume == 1 ? " dram of " : " drams of ")
                                ;
                            }
                            else
                            {
                                SB
                                    .Append("{{rules|")
                                    .Append(Volume.ToString("D6"))
                                    .Append("}}")
                                    .Append(Volume == 1 ? " dram of " : " drams of ")
                                ;
                            }
                        }
                        else
                        {
                            if (Stripped)
                            {
                                SB
                                    .Append(Volume)
                                    .Append(Volume == 1 ? " dram of " : " drams of ")
                                ;
                            }
                            else
                            {
                                SB
                                    .Append("{{rules|")
                                    .Append(Volume)
                                    .Append("}}")
                                    .Append(Volume == 1 ? " dram of " : " drams of ")
                                ;
                            }
                        }
                    }
                    AppendLiquidName(SB, Stripped: Stripped);
                }
                if (useSealed && ShowSeal)
                {
                    if (LiquidVisibleWhenSealed)
                    {
                        SB.Append(", ");
                    }
                    SB.Append(Stripped ? "sealed" : "{{c|sealed}}");
                }
                if (!Syntactic && UsePreposition.IsNullOrEmpty())
                {
                    SB.Append(Stripped ? "]" : "]}}");
                }
            }
        }

        public string GetLiquidDescription(
            bool IncludeAmount = true,
            bool IgnoreSeal = false,
            bool Stripped = false,
            bool Syntactic = false,
            bool ForSort = false,
            string UsePreposition = null
        )
        {
            StringBuilder s = Event.NewStringBuilder();
            AppendLiquidDescription(
                SB: s,
                IncludeAmount: IncludeAmount,
                IgnoreSeal: IgnoreSeal,
                Stripped: Stripped,
                Syntactic: Syntactic,
                ForSort: ForSort,
                UsePreposition: UsePreposition
            );
            return s.ToString();
        }

        public bool IsOpenVolume()
        {
            return (MaxVolume == -1);
        }

        public bool IsWadingDepth()
        {
            return IsOpenVolume() && Volume >= WADE_THRESHOLD;
        }

        public bool IsSwimmingDepth()
        {
            return IsOpenVolume() && Volume >= SWIM_THRESHOLD;
        }

        public bool IsSwimmableFor(GameObject who)
        {
            if (!IsOpenVolume())
            {
                return false;
            }
            if (!IsWadingDepth())
            {
                return false;
            }
            if (!IsSwimmingDepth())
            {
                if (Swimming.GetTargetMoveSpeedPenalty(who) > 0)
                {
                    return false;
                }
            }
            return true;
        }

        public void BaseRender()
        {
            if (ParentObject != null && IsOpenVolume())
            {
                if (Primary != null)
                {
                    RequirePrimaryLiquid().BaseRenderPrimary(this);
                }
                if (Secondary != null)
                {
                    RequireSecondaryLiquid().BaseRenderSecondary(this);
                }
            }
        }

        public override bool Render(RenderEvent E)
        {
            if (ParentObject != null && IsOpenVolume())
            {
                if (Primary != null)
                {
                    RequirePrimaryLiquid().RenderPrimary(this, E);
                }
                if (Secondary != null)
                {
                    RequireSecondaryLiquid().RenderSecondary(this, E);
                }
            }
            return true;
        }

        public string GetPrimaryLiquidColor()
        {
            if (Primary != null)
            {
                return RequirePrimaryLiquid().GetColor();
            }
            return null;
        }

        [NonSerialized] private static string[] PuddleRenderStrings = new string[]
        {
            ",",
            "`",
            "'",
            "" + (char) 7,
        };

        public bool CheckImage()
        {
            if (ParentObject == null || ParentObject.Render == null)
            {
                return false;
            }
            bool result = false;
            Cell C = ParentObject.CurrentCell;

            if (C != null)
            {
                LastPaintMask = -1;
                C.CheckPaintLiquidsAround(ParentObject);
            }
            if (IsWadingDepth())
            {
                if (!ParentObject.Render.RenderIfDark || ParentObject.Render.RenderString != "~")
                {
                    ParentObject.Render.RenderIfDark = true;
                    ParentObject.Render.RenderString = "~";
                    result = true;
                }
            }
            else
            if (IsOpenVolume())
            {
                if (ParentObject.Render.RenderIfDark)
                {
                    ParentObject.Render.RenderIfDark = false;
                    result = true;
                }
                string renderString = PuddleRenderStrings.GetRandomElement();
                if (ParentObject.Render.RenderString != renderString)
                {
                    ParentObject.Render.RenderString = PuddleRenderStrings.GetRandomElement();
                    result = true;
                }
            }
            string DetailColor = ParentObject.GetPropertyOrTag("DetailColorByLiquid");
            string ColorString = ParentObject.GetPropertyOrTag("ColorStringByLiquid");
            if (!DetailColor.IsNullOrEmpty() || !ColorString.IsNullOrEmpty())
            {
                bool TreatAsEmpty =
                    (Volume <= 0)
                    || (!LiquidVisibleWhenSealed && EffectivelySealed())
                    || (GetPrimaryLiquid() == null)
                ;
                if (TreatAsEmpty)
                {
                    if (!DetailColor.IsNullOrEmpty() && ParentObject.Render.DetailColor != DetailColor)
                    {
                        ParentObject.Render.DetailColor = DetailColor;
                        result = true;
                    }
                    if (!ColorString.IsNullOrEmpty() && ParentObject.Render.ColorString != ColorString)
                    {
                        ParentObject.Render.ColorString = ColorString;
                        result = true;
                    }
                }
                else
                {
                    string LiquidColor = GetPrimaryLiquid()?.GetColor();
                    if (!DetailColor.IsNullOrEmpty())
                    {
                        string useColor = LiquidColor ?? DetailColor;
                        string contrastShiftFrom = ParentObject.GetPropertyOrTag("DetailColorByLiquidContrastShiftFrom");
                        if (contrastShiftFrom != null)
                        {
                            string contrastShiftTo = ParentObject.GetPropertyOrTag("DetailColorByLiquidContrastShiftTo");
                            if (useColor == contrastShiftFrom)
                            {
                                useColor = contrastShiftTo;
                            }
                        }
                        if (ParentObject.Render.DetailColor != useColor)
                        {
                            ParentObject.Render.DetailColor = useColor;
                            result = true;
                        }
                    }
                    if (!ColorString.IsNullOrEmpty())
                    {
                        string useColor = LiquidColor.IsNullOrEmpty() ? ColorString : "&" + LiquidColor;
                        string contrastShiftFrom = ParentObject.GetPropertyOrTag("ColorStringByLiquidContrastShiftFrom");
                        if (contrastShiftFrom != null)
                        {
                            string contrastShiftTo = ParentObject.GetPropertyOrTag("ColorStringByLiquidContrastShiftTo");
                            if (useColor == contrastShiftFrom)
                            {
                                useColor = contrastShiftTo;
                            }
                        }
                        if (ParentObject.Render.ColorString != useColor)
                        {
                            ParentObject.Render.ColorString = useColor;
                            result = true;
                        }
                    }
                }
            }
            return result;
        }

        public bool UseDrams(int Num)
        {
            if (Volume > Num)
            {
                Volume -= Num;
                FlushWeightCaches();
                return true;
            }
            else
            {
                Empty();
                CheckImage();
                return false;
            }
        }

        public bool UseDram()
        {
            return UseDrams(1);
        }

        [NonSerialized] private static LiquidVolume consumeVolume;

        public bool UseDrams(Dictionary<string, int> Amounts)
        {
            if (consumeVolume == null)
            {
                consumeVolume = new LiquidVolume();
            }
            consumeVolume.ComponentLiquids.Clear();
            bool Result = true;
            int Consume = 0;
            foreach (KeyValuePair<string, int> KV in Amounts)
            {
                int Proportion;
                if (ComponentLiquids.TryGetValue(KV.Key, out Proportion))
                {
                    int Available = Math.Max(Volume * Proportion / 1000, 1);
                    int Amount = Math.Min(KV.Value, Available);
                    Consume += Amount;
                    consumeVolume.ComponentLiquids.Add(KV.Key, Amount);
                    if (Available < KV.Value)
                    {
                        Result = false;
                    }
                }
                else
                {
                    Result = false;
                }
            }
            if (Consume > 0)
            {
                consumeVolume.MaxVolume = Consume;
                consumeVolume.Volume = -Consume;
                consumeVolume.NormalizeProportions();
                MixWith(consumeVolume);
                FlushNavigationCaches();
                CheckGroundLiquidMerge();
            }
            return Result;
        }

        public bool UseDrams(string Liquid, int Amount)
        {
            bool Result = true;
            int Consume = 0;
            int Proportion;
            if (ComponentLiquids.TryGetValue(Liquid, out Proportion))
            {
                int Available = Math.Max(Volume * Proportion / 1000, 1);
                if (Available < Amount)
                {
                    Consume = Available;
                    Result = false;
                }
                else
                {
                    Consume = Amount;
                }
            }
            else
            {
                Result = false;
            }
            if (Consume > 0)
            {
                if (consumeVolume == null)
                {
                    consumeVolume = new LiquidVolume();
                }
                consumeVolume.ComponentLiquids.Clear();
                if (!consumeVolume.ComponentLiquids.ContainsKey(Liquid) || consumeVolume.ComponentLiquids.Count > 1)
                {
                    if (consumeVolume.ComponentLiquids.Count > 0)
                    {
                        consumeVolume.ComponentLiquids.Clear();
                    }
                    consumeVolume.ComponentLiquids.Add(Liquid, 1000);
                }
                consumeVolume.MaxVolume = Consume;
                consumeVolume.Volume = -Consume;
                MixWith(consumeVolume);
                FlushNavigationCaches();
                CheckGroundLiquidMerge();
            }
            return Result;
        }

        [NonSerialized] private static Dictionary<string, int> EvaporativityProportions = new Dictionary<string, int>();
        [NonSerialized] private static Dictionary<string, int> EvaporativityAmounts = new Dictionary<string, int>();

        public bool UseDramsByEvaporativity(int Num)
        {
            if (ComponentLiquids.Count < 2)
            {
                foreach (string Liquid in ComponentLiquids.Keys)
                {
                    if (GetLiquid(Liquid).Evaporativity > 0)
                    {
                        return UseDrams(Num);
                    }
                }
                return false;
            }
            else
            {
                EvaporativityProportions.Clear();
                EvaporativityAmounts.Clear();
                int total = 0;
                foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                {
                    int prop = GetLiquid(KV.Key).Evaporativity * KV.Value;
                    EvaporativityProportions.Add(KV.Key, prop);
                    total += prop;
                }
                int assigned = 0;
                foreach (KeyValuePair<string, int> KV in EvaporativityProportions)
                {
                    int amount = Num * KV.Value / total;
                    EvaporativityAmounts.Add(KV.Key, amount);
                    assigned += amount;
                }
                while (assigned < Num)
                {
                    string highest = null;
                    int high = 0;
                    foreach (KeyValuePair<string, int> KV in EvaporativityProportions)
                    {
                        if (KV.Value > high)
                        {
                            highest = KV.Key;
                            high = KV.Value;
                        }
                    }
                    if (highest == null)
                    {
                        break;
                    }
                    else
                    {
                        EvaporativityAmounts[highest]++;
                        assigned++;
                        EvaporativityProportions.Remove(highest);
                    }
                }
                UseDrams(EvaporativityAmounts);
                return true;
            }
        }

        [NonSerialized] private static Dictionary<string, int> StainingProportions = new Dictionary<string, int>();
        [NonSerialized] private static Dictionary<string, int> StainingAmounts = new Dictionary<string, int>();

        public bool Stain(GameObject obj, int Num)
        {
            StainingProportions.Clear();
            StainingAmounts.Clear();
            int total = 0;
            foreach (KeyValuePair<string, int> KV in ComponentLiquids)
            {
                BaseLiquid L = GetLiquid(KV.Key);
                if (!L.StainOnlyWhenPure || ComponentLiquids.Count == 1)
                {
                    int prop = L.Staining * KV.Value;
                    StainingProportions.Add(KV.Key, prop);
                    total += prop;
                }
            }
            for (int i = 0; i < Num; i++)
            {
                string Liquid = StainingProportions.GetRandomElement(ref total);
                if (Liquid == null)
                {
                    return false;
                }
                if (StainingAmounts.ContainsKey(Liquid))
                {
                    StainingAmounts[Liquid]++;
                }
                else
                {
                    StainingAmounts.Add(Liquid, 1);
                }
            }
            UseDrams(StainingAmounts);
            return obj.ForceApplyEffect(new LiquidStained(new LiquidVolume(StainingAmounts)));
        }

        public double GetLiquidExtrinsicValuePerDram()
        {
            switch (ComponentLiquids.Count)
            {
                case 0:
                    break;
                case 1:
                    if (Primary == null)
                    {
                        foreach (string b in ComponentLiquids.Keys)
                        {
                            return Liquids[b].GetExtrinsicValuePerDram(Pure: true);
                        }
                    }
                    else
                    {
                        return RequirePrimaryLiquid().GetExtrinsicValuePerDram(Pure: true);
                    }
                    break;
                default:
                {
                    double total = 0;
                    foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                    {
                        total += Liquids[KV.Key].GetExtrinsicValuePerDram(Pure: false) * KV.Value;
                    }
                    return total / 1000;
                }
            }
            return 0;
        }

        public int GetLiquidTemperature()
        {
            switch (ComponentLiquids.Count)
            {
                case 0:
                    break;
                case 1:
                    if (Primary == null)
                    {
                        foreach (string b in ComponentLiquids.Keys)
                        {
                            return Liquids[b].Temperature;
                        }
                    }
                    else
                    {
                        return RequirePrimaryLiquid().Temperature;
                    }
                    break;
                default:
                {
                    int Total = 0;
                    foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                    {
                        Total += Liquids[KV.Key].Temperature * KV.Value;
                    }
                    return Total / 1000;
                }
            }
            return 25;
        }

        public int GetLiquidFlameTemperature()
        {
            switch (ComponentLiquids.Count)
            {
                case 0:
                    break;
                case 1:
                    if (Primary == null)
                    {
                        foreach (string liquid in ComponentLiquids.Keys)
                        {
                            return Liquids[liquid].FlameTemperature;
                        }
                    }
                    else
                    {
                        return RequirePrimaryLiquid().FlameTemperature;
                    }
                    break;
                default:
                {
                    int Total = 0;
                    foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                    {
                        Total += Liquids[KV.Key].FlameTemperature * KV.Value;
                    }
                    return Total / 1000;
                }
            }
            return 0;
        }

        public int GetLiquidVaporTemperature()
        {
            switch (ComponentLiquids.Count)
            {
                case 0:
                    break;
                case 1:
                    if (Primary == null)
                    {
                        foreach (string liquid in ComponentLiquids.Keys)
                        {
                            return Liquids[liquid].VaporTemperature;
                        }
                    }
                    else
                    {
                        return RequirePrimaryLiquid().VaporTemperature;
                    }
                    break;
                default:
                {
                    int Total = 0;
                    foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                    {
                        Total += Liquids[KV.Key].VaporTemperature * KV.Value;
                    }
                    return Total / 1000;
                }
            }
            return 0;
        }

        public int GetLiquidFreezeTemperature()
        {
            switch (ComponentLiquids.Count)
            {
                case 0:
                    break;
                case 1:
                    if (Primary == null)
                    {
                        foreach (string liquid in ComponentLiquids.Keys)
                        {
                            return Liquids[liquid].FreezeTemperature;
                        }
                    }
                    else
                    {
                        return RequirePrimaryLiquid().FreezeTemperature;
                    }
                    break;
                default:
                {
                    int Total = 0;
                    foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                    {
                        Total += Liquids[KV.Key].FreezeTemperature * KV.Value;
                    }
                    return Total / 1000;
                }
            }
            return 0;
        }

        public int GetLiquidBrittleTemperature()
        {
            switch (ComponentLiquids.Count)
            {
                case 0:
                    break;
                case 1:
                    if (Primary == null)
                    {
                        foreach (string liquid in ComponentLiquids.Keys)
                        {
                            return Liquids[liquid].BrittleTemperature;
                        }
                    }
                    else
                    {
                        return RequirePrimaryLiquid().BrittleTemperature;
                    }
                    break;
                default:
                {
                    int Total = 0;
                    foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                    {
                        Total += Liquids[KV.Key].BrittleTemperature * KV.Value;
                    }
                    return Total / 1000;
                }
            }
            return 0;
        }

        public int GetLiquidElectricalConductivity()
        {
            switch (ComponentLiquids.Count)
            {
                case 0:
                    break;
                case 1:
                    if (Primary == null)
                    {
                        foreach (string b in ComponentLiquids.Keys)
                        {
                            return Liquids[b].PureElectricalConductivity;
                        }
                    }
                    else
                    {
                        return RequirePrimaryLiquid().PureElectricalConductivity;
                    }
                    break;
                default:
                {
                    int Total = 0;
                    foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                    {
                        Total += Liquids[KV.Key].MixedElectricalConductivity * KV.Value;
                    }
                    return Total / 1000;
                }
            }
            return 80;
        }

        public string GetLiquidFreezeObject()
        {
            return GetPrimaryLiquid()?.GetFreezeObjectForVolume(Amount(Primary));
        }

        public string GetLiquidFreezeObject(out string FreezeVerb)
        {
            FreezeVerb = null;
            return GetPrimaryLiquid()?.GetFreezeObjectForVolume(Amount(Primary), out FreezeVerb);
        }

        public void GetLiquidPhysicalProperties(
            out int FlameTemperature,
            out int VaporTemperature,
            out int FreezeTemperature,
            out int BrittleTemperature,
            out int ElectricalConductivity
        )
        {
            FlameTemperature = 99999;
            VaporTemperature = 350;
            FreezeTemperature = 0;
            BrittleTemperature = -100;
            ElectricalConductivity = 80;
            switch (ComponentLiquids.Count)
            {
                case 0:
                    break;
                case 1:
                    if (Primary == null)
                    {
                        foreach (string liquid in ComponentLiquids.Keys)
                        {
                            BaseLiquid Liquid = Liquids[liquid];
                            FlameTemperature = Liquid.FlameTemperature;
                            VaporTemperature = Liquid.VaporTemperature;
                            FreezeTemperature = Liquid.FreezeTemperature;
                            BrittleTemperature = Liquid.BrittleTemperature;
                            ElectricalConductivity = Liquid.PureElectricalConductivity;
                            break;
                        }
                    }
                    else
                    {
                        BaseLiquid Liquid = RequirePrimaryLiquid();
                        FlameTemperature = Liquid.FlameTemperature;
                        VaporTemperature = Liquid.VaporTemperature;
                        FreezeTemperature = Liquid.FreezeTemperature;
                        BrittleTemperature = Liquid.BrittleTemperature;
                        ElectricalConductivity = Liquid.PureElectricalConductivity;
                    }
                    break;
                default:
                {
                    FlameTemperature = 0;
                    VaporTemperature = 0;
                    FreezeTemperature = 0;
                    BrittleTemperature = 0;
                    ElectricalConductivity = 0;
                    foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                    {
                        BaseLiquid Liquid = Liquids[KV.Key];
                        FlameTemperature += Liquid.FlameTemperature * KV.Value;
                        VaporTemperature += Liquid.VaporTemperature * KV.Value;
                        FreezeTemperature += Liquid.FreezeTemperature * KV.Value;
                        BrittleTemperature += Liquid.BrittleTemperature * KV.Value;
                        ElectricalConductivity += Liquid.MixedElectricalConductivity * KV.Value;
                    }
                    FlameTemperature /= 1000;
                    VaporTemperature /= 1000;
                    FreezeTemperature /= 1000;
                    BrittleTemperature /= 1000;
                    ElectricalConductivity /= 1000;
                    break;
                }
            }
        }

        public int GetLiquidThermalConductivity()
        {
            switch (ComponentLiquids.Count)
            {
                case 0:
                    break;
                case 1:
                    if (Primary == null)
                    {
                        foreach (string b in ComponentLiquids.Keys)
                        {
                            return Liquids[b].ThermalConductivity;
                        }
                    }
                    else
                    {
                        return RequirePrimaryLiquid().ThermalConductivity;
                    }
                    break;
                default:
                {
                    int Total = 0;
                    foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                    {
                        Total += Liquids[KV.Key].ThermalConductivity * KV.Value;
                    }
                    return Total / 1000;
                }
            }
            return 50;
        }

        public int GetLiquidCombustibility()
        {
            switch (ComponentLiquids.Count)
            {
                case 0:
                    break;
                case 1:
                    if (Primary == null)
                    {
                        foreach (string b in ComponentLiquids.Keys)
                        {
                            return Liquids[b].Combustibility;
                        }
                    }
                    else
                    {
                        return RequirePrimaryLiquid().Combustibility;
                    }
                    break;
                default:
                {
                    int Total = 0;
                    foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                    {
                        Total += Liquids[KV.Key].Combustibility * KV.Value;
                    }
                    return Total / 1000;
                }
            }
            return 0;
        }

        public float GetLiquidCooling()
        {
            int temperature = GetLiquidTemperature();
            int thermalConductivity = GetLiquidThermalConductivity();
            int combustibility = GetLiquidCombustibility();
            float cooling = 0.125f + ((1000f - temperature) / 1000f);
            if (combustibility > 0)
            {
                cooling = cooling * (100 - combustibility) / 100;
            }
            if (thermalConductivity != 50)
            {
                cooling = cooling * thermalConductivity / 50;
            }
            return cooling;
        }

        public int GetLiquidAdsorbence()
        {
            switch (ComponentLiquids.Count)
            {
                case 0:
                    break;
                case 1:
                    if (Primary == null)
                    {
                        foreach (string b in ComponentLiquids.Keys)
                        {
                            return Liquids[b].Adsorbence;
                        }
                    }
                    else
                    {
                        return RequirePrimaryLiquid().Adsorbence;
                    }
                    break;
                default:
                {
                    int Total = 0;
                    foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                    {
                        Total += Liquids[KV.Key].Adsorbence * KV.Value;
                    }
                    if (Total == 0)
                    {
                        return 0;
                    }
                    return Math.Max(Total / 1000, 1);
                }
            }
            return 0;
        }

        public int GetLiquidFluidity()
        {
            switch (ComponentLiquids.Count)
            {
                case 0:
                    break;
                case 1:
                    if (Primary == null)
                    {
                        foreach (string b in ComponentLiquids.Keys)
                        {
                            return Liquids[b].Fluidity;
                        }
                    }
                    else
                    {
                        return RequirePrimaryLiquid().Fluidity;
                    }
                    break;
                default:
                {
                    int Total = 0;
                    foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                    {
                        Total += Liquids[KV.Key].Fluidity * KV.Value;
                    }
                    if (Total == 0)
                    {
                        return 0;
                    }
                    return Math.Max(Total / 1000, 1);
                }
            }
            return 0;
        }

        public int GetLiquidViscosity()
        {
            return 100 - GetLiquidFluidity();
        }

        public int GetLiquidEvaporativity()
        {
            switch (ComponentLiquids.Count)
            {
                case 0:
                    break;
                case 1:
                    if (Primary == null)
                    {
                        foreach (string b in ComponentLiquids.Keys)
                        {
                            return Liquids[b].Evaporativity;
                        }
                    }
                    else
                    {
                        return RequirePrimaryLiquid().Evaporativity;
                    }
                    break;
                default:
                {
                    int Total = 0;
                    foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                    {
                        Total += Liquids[KV.Key].Evaporativity * KV.Value;
                    }
                    if (Total == 0)
                    {
                        return 0;
                    }
                    return Math.Max(Total / 1000, 1);
                }
            }
            return 0;
        }

        public int GetLiquidStaining()
        {
            switch (ComponentLiquids.Count)
            {
                case 0:
                    break;
                case 1:
                    if (Primary == null)
                    {
                        foreach (string b in ComponentLiquids.Keys)
                        {
                            return Liquids[b].Staining;
                        }
                    }
                    else
                    {
                        return RequirePrimaryLiquid().Staining;
                    }
                    break;
                default:
                {
                    int Total = 0;
                    foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                    {
                        BaseLiquid L = Liquids[KV.Key];
                        if (!L.StainOnlyWhenPure)
                        {
                            Total += L.Staining * KV.Value;
                        }
                    }
                    if (Total == 0)
                    {
                        return 0;
                    }
                    return Math.Max(Total / 1000, 1);
                }
            }
            return 0;
        }

        public int GetLiquidCleansing()
        {
            switch (ComponentLiquids.Count)
            {
                case 0:
                    break;
                case 1:
                    if (Primary == null)
                    {
                        foreach (string b in ComponentLiquids.Keys)
                        {
                            return Liquids[b].Cleansing;
                        }
                    }
                    else
                    {
                        return RequirePrimaryLiquid().Cleansing;
                    }
                    break;
                default:
                {
                    int Total = 0;
                    foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                    {
                        Total += Liquids[KV.Key].Cleansing * KV.Value;
                    }
                    if (Total == 0)
                    {
                        return 0;
                    }
                    return Math.Max(Total / 1000, 1);
                }
            }
            return 0;
        }

        public bool IsLiquidUsableForCleaning()
        {
            switch (ComponentLiquids.Count)
            {
                case 0:
                    break;
                case 1:
                    if (Primary == null)
                    {
                        foreach (string LiquidID in ComponentLiquids.Keys)
                        {
                            if (Liquids[LiquidID].EnableCleaning)
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        if (RequirePrimaryLiquid().EnableCleaning)
                        {
                            return true;
                        }
                    }
                    break;
                default:
                {
                    int Total = 0;
                    bool Enabled = false;
                    foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                    {
                        BaseLiquid L = Liquids[KV.Key];
                        Total += L.Cleansing * KV.Value;
                        if (L.EnableCleaning)
                        {
                            Enabled = true;
                        }
                    }
                    if (Enabled && Total > 0 && Math.Max(Total / 1000, 1) >= CLEANING_MIXTURE_THRESHOLD)
                    {
                        return true;
                    }
                    break;
                }
            }
            return false;
        }

        public bool UsableForCleaning()
        {
            if (ParentObject == null)
            {
                return false;
            }
            if (Volume <= 0)
            {
                return false;
            }
            if (!IsLiquidUsableForCleaning())
            {
                return false;
            }
            if (EffectivelySealed())
            {
                return false;
            }
            return true;
        }

        public bool SafeContainer(GameObject obj)
        {
            if (ComponentLiquids.Count == 1 && Primary != null)
            {
                return RequirePrimaryLiquid().SafeContainer(obj);
            }
            foreach (string LiquidID in ComponentLiquids.Keys)
            {
                if (!Liquids[LiquidID].SafeContainer(obj))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsGameObjectSafeContainerForLiquid(GameObject obj, string liquid)
        {
            try
            {
                if (liquid.Contains(','))
                {
                    foreach (string part in liquid.Split(','))
                    {
                        if (part.Contains('-'))
                        {
                            if (!GetLiquid(Liquids[part.Split('-')[0]].ID).SafeContainer(obj))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (!GetLiquid(Liquids[part].ID).SafeContainer(obj))
                            {
                                return false;
                            }
                        }
                    }
                }
                else
                if (liquid.Contains('-'))
                {
                    if (!GetLiquid(Liquids[liquid.Split('-')[0]].ID).SafeContainer(obj))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!GetLiquid(Liquids[liquid].ID).SafeContainer(obj))
                    {
                        return false;
                    }
                }
            }
            catch
            {
                MetricsManager.LogError("invalid liquid specification " + (liquid ?? "NULL"));
            }
            return true;
        }

        public bool ConsiderLiquidDangerousToContact()
        {
            if (ComponentLiquids.Count == 1 && Primary != null)
            {
                return RequirePrimaryLiquid().ConsiderDangerousToContact;
            }
            foreach (string LiquidID in ComponentLiquids.Keys)
            {
                if (Liquids[LiquidID].ConsiderDangerousToContact)
                {
                    return true;
                }
            }
            return false;
        }

        public bool ConsiderLiquidDangerousToDrink()
        {
            if (ComponentLiquids.Count == 1 && Primary != null)
            {
                return RequirePrimaryLiquid().ConsiderDangerousToDrink;
            }
            foreach (string LiquidID in ComponentLiquids.Keys)
            {
                if (Liquids[LiquidID].ConsiderDangerousToDrink)
                {
                    return true;
                }
            }
            return false;
        }

        public double GetLiquidWeightPerDram()
        {
            switch (ComponentLiquids.Count)
            {
                case 0:
                    break;
                case 1:
                    if (Primary == null)
                    {
                        foreach (string b in ComponentLiquids.Keys)
                        {
                            return Liquids[b].Weight;
                        }
                    }
                    else
                    {
                        return RequirePrimaryLiquid().Weight;
                    }
                    break;
                default:
                {
                    double Total = 0;
                    foreach (KeyValuePair<string, int> KV in ComponentLiquids)
                    {
                        Total += Liquids[KV.Key].Weight * KV.Value;
                    }
                    return Total / 1000;
                }
            }
            return 0;
        }

        public string GetCirculatoryLossTerm()
        {
            return RequirePrimaryLiquid().CirculatoryLossTerm;
        }

        public string GetColoredCirculatoryLossTerm()
        {
            return RequirePrimaryLiquid().ColoredCirculatoryLossTerm;
        }

        public string GetCirculatoryLossNoun()
        {
            return RequirePrimaryLiquid().CirculatoryLossNoun;
        }

        public string GetColoredCirculatoryLossNoun()
        {
            return RequirePrimaryLiquid().ColoredCirculatoryLossNoun;
        }

        private bool PourIntoCellInternal(
            GameObject Pourer,
            Cell TargetCell,
            int PourAmount,
            bool CanPourOn,
            bool Pure,
            bool CanMergeWithGroundLiquid,
            ref bool RequestInterfaceExit,
            bool UsePerformancePath = false
        )
        {
            if (ParentObject != null && ParentObject.IsTemporary)
            {
                Volume -= PourAmount;
                return false;
            }
            if (TargetCell == null || TargetCell.OnWorldMap())
            {
                return false;
            }
            foreach (var pair in ComponentLiquids)
            {
                if (!Liquids[pair.Key].PourIntoCell(
                    Liquid: this,
                    Pourer: Pourer,
                    TargetCell: TargetCell,
                    PourAmount: ref PourAmount,
                    CanPourOn: CanPourOn,
                    RequestInterfaceExit: ref RequestInterfaceExit
                ))
                {
                    return false;
                }
            }
            bool cellSolid = TargetCell.IsSolidFor(Pourer);
            restart:
            if (CanPourOn)
            {
                GameObject Target = TargetCell.GetCombatTarget(Pourer);
                if (Target != null)
                {
                    LiquidVolume V = Target.LiquidVolume;
                    if (V == null || (!V.IsOpenVolume() && !V.Collector))
                    {
                        PourAmount -= ProcessContact(Target, Initial: true, Poured: true, PouredBy: Pourer, ContactVolume: PourAmount);
                        if (PourAmount <= 0)
                        {
                            return true;
                        }
                    }
                }
                for (int i = 0, j = TargetCell.Objects.Count; i < j; i++)
                {
                    GameObject Object = TargetCell.Objects[i];
                    if (
                        Object != ParentObject
                        && Object != Pourer
                        && Object != Target
                        && (Pourer == null || Pourer.PhaseMatches(Object))
                        && ((Pourer != null && Pourer.IsFlying) || !Object.IsFlying)
                    )
                    {
                        LiquidVolume V = Object.LiquidVolume;
                        if (V == null || (!V.IsOpenVolume() && !V.Collector))
                        {
                            PourAmount -= ProcessContact(Object, Initial: true, Poured: true, PouredBy: Pourer, ContactVolume: PourAmount);
                            if (PourAmount <= 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            // not doing an i, j loop here because sometimes the object list gets permuted unpredictably
            for (int i = 0; i < TargetCell.Objects.Count; i++)
            {
                GameObject Object = TargetCell.Objects[i];
                if (Object != ParentObject && (!cellSolid || Object.CanInteractInCellWithSolid(Pourer)))
                {
                    LiquidVolume V = Object.LiquidVolume;
                    if (V != null && V.IsOpenVolume())
                    {
                        Temporary pTemporary = Object.GetPart<Temporary>();
                        ExistenceSupport pEx = Object.GetPart<ExistenceSupport>();
                        if (pTemporary != null)
                        {
                            pTemporary.Expire(Silent: true);
                            if (!GameObject.Validate(Object))
                            {
                                goto restart;
                            }
                        }
                        else
                        if (pEx != null)
                        {
                            pEx.Unsupported(Silent: true);
                            if (!GameObject.Validate(Object))
                            {
                                goto restart;
                            }
                        }
                        else
                        {
                            MixInto(V, PourAmount, ref RequestInterfaceExit, Pourer);
                            return true;
                        }
                    }
                }
            }
            // not doing an i, j loop here because sometimes the object list gets permuted unpredictably
            for (int i = 0; i < TargetCell.Objects.Count; i++)
            {
                GameObject Object = TargetCell.Objects[i];
                if (Object != ParentObject && (!cellSolid || Object.CanInteractInCellWithSolid(Pourer)))
                {
                    LiquidVolume V = Object.LiquidVolume;
                    if (V != null && V != this && !V.IsOpenVolume() && V.Collector && V.Volume < V.MaxVolume)
                    {
                        int AvailableSpace = V.MaxVolume - V.Volume;
                        if (AvailableSpace >= PourAmount)
                        {
                            MixInto(V, PourAmount, ref RequestInterfaceExit, Pourer);
                            return true;
                        }
                        else
                        {
                            MixInto(V, AvailableSpace, ref RequestInterfaceExit, Pourer);
                            PourAmount -= AvailableSpace;
                        }
                    }
                }
            }
            // not doing an i, j loop here because sometimes the object list gets permuted unpredictably
            for (int i = 0; i < TargetCell.Objects.Count; i++)
            {
                GameObject Object = TargetCell.Objects[i];
                if (Object != ParentObject && (!cellSolid || Object.CanInteractInCellWithSolid(Pourer)))
                {
                    LiquidVolume V = Object.LiquidVolume;
                    if (V != null && V.IsOpenVolume())
                    {
                        if (V != this)
                        {
                            Temporary pTemporary = Object.GetPart<Temporary>();
                            ExistenceSupport pEx = Object.GetPart<ExistenceSupport>();
                            if (pTemporary != null)
                            {
                                pTemporary.Expire(Silent: true);
                                if (!GameObject.Validate(Object))
                                {
                                    goto restart;
                                }
                            }
                            else
                            if (pEx != null)
                            {
                                pEx.Unsupported(Silent: true);
                                if (!GameObject.Validate(Object))
                                {
                                    goto restart;
                                }
                            }
                            else
                            {
                                MixInto(V, PourAmount, ref RequestInterfaceExit, Pourer);
                            }
                        }
                        return true;
                    }
                }
            }
            string GroundLiquid = TargetCell.GroundLiquid;
            if (CanMergeWithGroundLiquid && !GroundLiquid.IsNullOrEmpty() && IsPureLiquid(GroundLiquid))
            {
                Volume -= PourAmount;
                if (Volume <= 0)
                {
                    Empty();
                    CheckImage();
                }
                return true;
            }

            if( UsePerformancePath && TargetCell.HasOpenLiquidVolume() )
            {
                var groundVolume = TargetCell.GetOpenLiquidVolume();
                groundVolume.GetPart<LiquidVolume>().MixWith(this, Amount:PourAmount, UseTempSplit:true);
                Volume -= PourAmount;
                if( Volume <= 0 )
                {
                    Empty();
                    CheckImage();
                }
                return true;
            }

            GameObject NewVolume = GameObject.Create("Water");
            LiquidVolume pVolume = NewVolume.LiquidVolume;
            if (Pure || GroundLiquid.IsNullOrEmpty())
            {
                pVolume.Volume = 0;
                MixInto(pVolume, PourAmount, ref RequestInterfaceExit, Pourer);
            }
            else
            {
                // dilute 50% with the ground liquid but hold volume constant
                pVolume.InitialLiquid = GroundLiquid;
                pVolume.Volume = PourAmount;
                MixInto(pVolume, PourAmount, ref RequestInterfaceExit, Pourer);
                pVolume.Volume = PourAmount;
            }
            if (Pourer != null)
            {
                Phase.carryOver(Pourer, NewVolume, 1);
            }

            TargetCell.AddObject(
                NewVolume,
                Type: CanPourOn ? "Pour" : "Flow",
                Actor: Pourer
            );
            return true;
        }

        public bool PourIntoCell(
            GameObject Pourer,
            Cell TargetCell,
            int PourAmount,
            ref bool RequestInterfaceExit,
            bool CanPourOn = true,
            bool Pure = false,
            bool CanMergeWithGroundLiquid = false,
            bool UsePerformancePath = false
        )
        {
            bool Result = PourIntoCellInternal(
                Pourer: Pourer,
                TargetCell: TargetCell,
                PourAmount: PourAmount,
                CanPourOn: CanPourOn,
                Pure: Pure,
                CanMergeWithGroundLiquid: CanMergeWithGroundLiquid,
                RequestInterfaceExit: ref RequestInterfaceExit,
                UsePerformancePath: UsePerformancePath
            );
            if (Volume <= 0)
            {
                Empty();
            }
            CheckImage();
            return Result;
        }

        public bool PourIntoCell(
            GameObject Pourer,
            Cell TargetCell,
            int PourAmount,
            bool CanPourOn = true,
            bool Pure = false,
            bool CanMergeWithGroundLiquid = false,
            bool UsePerformancePath = false
        )
        {
            bool RequestInterfaceExit = false;
            return PourIntoCell(
                Pourer: Pourer,
                TargetCell: TargetCell,
                PourAmount: PourAmount,
                RequestInterfaceExit: ref RequestInterfaceExit,
                CanPourOn: CanPourOn,
                Pure: Pure,
                CanMergeWithGroundLiquid: CanMergeWithGroundLiquid,
                UsePerformancePath: UsePerformancePath
            );
        }

        public bool PourIntoCell(Cell TargetCell, int PourAmount, bool CanPourOn = true, bool Pure = false, bool CanMergeWithGroundLiquid = false)
        {
            return PourIntoCell(null, TargetCell, PourAmount, CanPourOn, Pure, CanMergeWithGroundLiquid);
        }

        public bool FlowIntoCell(
            int Amount = -1,
            Cell TargetCell = null,
            GameObject Pourer = null,
            bool CanPourOn = false,
            bool Pure = false,
            bool CanMergeWithGroundLiquid = true
        )
        {
            if (Amount == -1)
            {
                Amount = Volume;
            }
            if (TargetCell == null && ParentObject != null)
            {
                TargetCell = ParentObject.GetCurrentCell();
            }
            return PourIntoCell(Pourer, TargetCell, Amount, CanPourOn, Pure, CanMergeWithGroundLiquid,  UsePerformancePath:true );
        }

        private bool MixInto(
            LiquidVolume V,
            int Amount,
            ref bool RequestInterfaceExit,
            GameObject PouredBy = null
        )
        {
            bool result = false;
            if (V != this)
            {
                if (Amount < Volume)
                {
                    V.MixWith(
                        Liquid: this,
                        Amount: Amount,
                        UseTempSplit: true,
                        PouredFrom: ParentObject,
                        PouredBy: PouredBy,
                        RequestInterfaceExit: ref RequestInterfaceExit
                    );
                }
                else
                {
                    result = V.MixWith(
                        Liquid: this,
                        PouredFrom: ParentObject,
                        PouredBy: PouredBy,
                        RequestInterfaceExit: ref RequestInterfaceExit
                    );
                    if (result)
                    {
                        Empty();
                    }
                }
            }
            return result;
        }

        private bool MixInto(
            LiquidVolume V,
            int Amount,
            GameObject PouredBy = null
        )
        {
            bool RequestInterfaceExit = false;
            return MixInto(
                V: V,
                Amount: Amount,
                RequestInterfaceExit: ref RequestInterfaceExit,
                PouredBy: PouredBy
            );
        }

        /// <summary>
        /// Mingles this liquid volume with another as appropriate to them being
        /// adjacent and free to intermix, as if you had a divider between two
        /// halves of a bottle then removed it.
        /// </summary>
        /// <param Name="other">
        /// The other liquid volume to mingle with.
        /// </param>
        public void MingleAdjacent(LiquidVolume other)
        {
            int myEquilibriumVolume;
            int otherEquilibriumVolume;
            if (Volume == other.Volume && MaxVolume == other.MaxVolume)
            {
                myEquilibriumVolume = Volume;
                otherEquilibriumVolume = other.Volume;
            }
            else
            if (Volume == MaxVolume && other.Volume == other.MaxVolume)
            {
                myEquilibriumVolume = Volume;
                otherEquilibriumVolume = other.Volume;
            }
            else
            {
                int totalMaxVolume = MaxVolume + other.MaxVolume;
                int totalVolume = Volume + other.Volume;
                if (totalVolume <= 1)
                {
                    return;
                }
                if (IsOpenVolume() || other.IsOpenVolume())
                {
                    myEquilibriumVolume = otherEquilibriumVolume = totalVolume / 2;
                }
                else
                {
                    myEquilibriumVolume = totalVolume * MaxVolume / totalMaxVolume;
                    otherEquilibriumVolume = totalVolume * other.MaxVolume / totalMaxVolume;
                }
                if (myEquilibriumVolume + otherEquilibriumVolume < totalVolume)
                {
                    if (string.Compare(ParentObject.ID, other.ParentObject.ID) > 0)
                    {
                        myEquilibriumVolume++;
                    }
                    else
                    {
                        otherEquilibriumVolume++;
                    }
                }
            }
            if (Volume < myEquilibriumVolume)
            {
                MixWith(
                    Liquid: other,
                    Amount: myEquilibriumVolume - Volume,
                    PouredFrom: other.ParentObject
                );
            }
            else
            if (other.Volume < otherEquilibriumVolume)
            {
                other.MixWith(
                    Liquid: this,
                    Amount: otherEquilibriumVolume - other.Volume,
                    PouredFrom: other.ParentObject
                );
            }
            else
            if (!ComponentLiquids.SameAs(other.ComponentLiquids))
            {
                int amount = Rules.Stat.Random(1, Math.Max(Math.Min(Volume, other.Volume) / 4, 1));
                MixWith(
                    Liquid: other,
                    Amount: amount,
                    PouredFrom: other.ParentObject
                );
                other.MixWith(
                    Liquid: this,
                    Amount: amount,
                    PouredFrom: ParentObject
                );
            }
        }

        [NonSerialized] private static Queue<List<string>> keylistPool = new Queue<List<string>>();

        List<string> getKeylist()
        {
            if (keylistPool.Count > 0)
            {
                return keylistPool.Dequeue();
            }
            return new List<string>(256);
        }

        public string GetActiveAutogetLiquid()
        {
            if (AutoCollectLiquidType != null)
            {
                return AutoCollectLiquidType;
            }
            if (Options.AutogetFreshWater)
            {
                return LiquidWater.ID;
            }
            return null;
        }

        public void Fill(string Liquid, int Drams)
        {
            Empty(WillCheckImage: true, NoDestroy: true);
            if (Drams <= 0)
            {
                CheckImage();
                return;
            }
            TrackAsLiquid(Liquid);
            Volume = Drams;
            Liquids[Liquid].FillingContainer(ParentObject, this);
            RecalculatePrimary();
            RecalculateProperties();
        }

        public bool AddDrams(string Liquid, int Drams)
        {
            if (Drams > 0)
            {
                int VolumeLeft = (IsOpenVolume() ? int.MaxValue : MaxVolume) - Volume;
                if (VolumeLeft > 0)
                {
                    bool WasEmpty = IsEmpty();
                    if (WasEmpty || IsPureLiquid(Liquid))
                    {
                        if (WasEmpty)
                        {
                            TrackAsLiquid(Liquid);
                        }
                        if (Drams < VolumeLeft)
                        {
                            Volume += Drams;
                        }
                        else
                        {
                            Volume += VolumeLeft;
                        }
                        if (ParentObject != null && !IsOpenVolume())
                        {
                            string[] Keys = new string[ComponentLiquids.Keys.Count];
                            ComponentLiquids.Keys.CopyTo(Keys, 0);
                            foreach (string l in Keys)
                            {
                                Liquids[l].FillingContainer(ParentObject, this);
                            }
                        }
                        RecalculatePrimary();
                        RecalculateProperties();
                    }
                    else
                    {
                        MixWith(new LiquidVolume(Liquid, Math.Min(Drams, VolumeLeft)));
                    }
                    return true;
                }
            }
            return false;
        }

        public bool GiveDrams(
            string Liquid,
            ref int Drams,
            bool Auto = false,
            List<GameObject> StoredIn = null,
            GameObject Actor = null
        )
        {
            if (
                Drams > 0
                && (IsPureLiquid(Liquid) || IsEmpty())
                && (!Auto || Liquid == GetActiveAutogetLiquid())
                && !EffectivelySealed()
                && ParentObject.AllowLiquidCollection(Liquid, actor: Actor)
            )
            {
                int VolumeLeft = MaxVolume - Volume;
                if (VolumeLeft > 0)
                {
                    bool WasEmpty = IsEmpty();
                    if (WasEmpty)
                    {
                        TrackAsLiquid(Liquid);
                    }
                    if (StoredIn != null && ParentObject != null)
                    {
                        StoredIn.Add(ParentObject);
                    }
                    if (Drams < VolumeLeft)
                    {
                        Volume += Drams;
                        Drams = 0;
                    }
                    else
                    {
                        Drams -= VolumeLeft;
                        Volume += VolumeLeft;
                    }
                    if (ParentObject != null && !IsOpenVolume())
                    {
                        string[] Keys = new string[ComponentLiquids.Keys.Count];
                        ComponentLiquids.Keys.CopyTo(Keys, 0);
                        foreach (string l in Keys)
                        {
                            Liquids[l].FillingContainer(ParentObject, this);
                        }
                    }
                    RecalculatePrimary();
                    RecalculateProperties();
                    return true;
                }
            }
            return false;
        }

        public bool GiveDrams(
            string Liquid,
            int Drams,
            bool Auto = false,
            List<GameObject> StoredIn = null,
            GameObject Actor = null
        )
        {
            return GiveDrams(Liquid, ref Drams, Auto, StoredIn, Actor);
        }

        public bool Pour(
            ref bool RequestInterfaceExit,
            GameObject Actor = null,
            Cell TargetCell = null,
            bool Forced = false,
            bool Douse = false,
            int PourAmount = -1,
            bool OwnershipHandled = false
        )
        {
            if (Forced || !EffectivelySealed())
            {
                if (ParentObject.IsInStasis())
                {
                    Actor?.Fail("You cannot seem to interact with " + ParentObject.t() + " in any way.");
                    return false;
                }
                if (!Forced && !OwnershipHandled && Actor != null && Actor.IsPlayer() && ParentObject.Owner != null)
                {
                    if (Popup.ShowYesNoCancel(ParentObject.IndicativeDistal + ParentObject.Is + " not owned by you. Are you sure you want to pour from " + ParentObject.them + "?") != DialogResult.Yes)
                    {
                        return false;
                    }
                    else
                    {
                        ParentObject.Physics.BroadcastForHelp(Actor);
                    }
                }

                Temporary pTemporary = ParentObject.GetPart<Temporary>();
                if (pTemporary != null)
                {
                    if (IsOpenVolume())
                    {
                        pTemporary.Expire();
                        if (GameObject.Validate(ParentObject))
                        {
                            Empty();
                        }
                    }
                    else
                    {
                        Empty();
                    }
                    return false;
                }
                ExistenceSupport pEx = ParentObject.GetPart<ExistenceSupport>();
                if (pEx != null)
                {
                    if (IsOpenVolume())
                    {
                        pEx.Unsupported();
                        if (GameObject.Validate(ParentObject))
                        {
                            Empty();
                        }
                    }
                    else
                    {
                        Empty();
                    }
                    return false;
                }

                if (TargetCell == null)
                {
                    if (!Forced && Actor != null && Actor.IsPlayer())
                    {
                        if (IsOpenVolume() || Actor.OnWorldMap())
                        {
                            return InventoryActionEvent.Check(
                                Object: ParentObject,
                                Actor: Actor,
                                Item: ParentObject,
                                Command: "Fill",
                                OwnershipHandled: true
                            );
                        }
                        int ichoice = Popup.PickOption(Intro: "Where do you want to pour " + ParentObject.t() + "?",
                            Options: new string[]
                            {
                                "Pour it into another container.",
                                "Pour it nearby.",
                                "Pour it on " + Actor.itself + ".",
                            }, Hotkeys: new char[] {
                                'a',
                                'b',
                                'c',
                            }, AllowEscape: true);
                        if (ichoice < 0)
                        {
                            return false;
                        }
                        if (ichoice == 0)
                        {
                            return InventoryActionEvent.Check(
                                Object: ParentObject,
                                Actor: Actor,
                                Item: ParentObject,
                                Command: "Fill",
                                OwnershipHandled: true
                            );
                        }
                        if (ichoice == 1)
                        {
                            Douse = false;
                            string Dir = UI.PickDirection.ShowPicker(Label: "Pour"); //TODO:TARGETLABEL
                            if (Dir == null)
                            {
                                return false;
                            }
                            TargetCell = Actor.GetCurrentCell().GetCellFromDirection(Dir, BuiltOnly: false);
                        }
                        if (ichoice == 2)
                        {
                            Douse = true;
                            TargetCell = Actor.GetCurrentCell();
                        }
                    }
                    else
                    {
                        if (IsOpenVolume())
                        {
                            return false;
                        }
                        Douse = true;
                        if (Actor != null)
                        {
                            TargetCell = Actor.GetCurrentCell();
                        }
                    }
                }

                if (PourAmount == -1)
                {
                    if (Forced)
                    {
                        PourAmount = Volume;
                    }
                    else
                    if (Actor != null && Actor.IsPlayer())
                    {
                        int? PourSpec = UI.Popup.AskNumber("How many drams? (max=" + Volume + ")", Start: Volume, Min: 0, Max: Volume);
                        try
                        {
                            PourAmount = Convert.ToInt32(PourSpec);
                        }
                        catch
                        {
                            return false;
                        }
                    }
                }
                if (PourAmount > Volume)
                {
                    PourAmount = Volume;
                }
                if (PourAmount <= 0)
                {
                    return false;
                }

                if (Volume > 0)
                {
                    if (Actor != null)
                    {
                        if (Douse)
                        {
                            foreach (var pair in ComponentLiquids)
                            {
                                if (!Liquids[pair.Key].Douse(this, Actor, ref PourAmount, ref RequestInterfaceExit))
                                {
                                    return false;
                                }
                            }
                            PlayWorldSound("Sounds/Interact/sfx_interact_liquidContainer_pourout");
                            if (Actor.IsPlayer())
                            {
                                Popup.Show(PourAmount.Things("dram") + " of " + GetLiquidName() + " pours out all over you!");
                            }
                            else
                            if (Actor.IsVisible())
                            {
                                if( Actor.IsVisible()) AddPlayerMessage(PourAmount.Things("dram") + " of " + GetLiquidName() + " pours out all over " + Actor.t() + "!");
                            }

                            PourAmount -= ProcessContact(
                                Actor,
                                Initial: true,
                                Poured: true,
                                PouredBy: Actor,
                                ContactVolume: PourAmount
                            );
                            if (PourAmount <= 0)
                            {
                                return true;
                            }
                        }
                    }
                    if (TargetCell != null)
                    {
                        if (!Douse && Actor != null)
                        {
                            if( Actor?.IsPlayer() ?? false )
                            {
                                PlayUISound("Sounds/Abilities/sfx_ability_generic_waterPour");
                            }
                            else
                            {
                                TargetCell?.PlayWorldSound("Sounds/Abilities/sfx_ability_generic_waterPour");
                            }

                            XDidY(Actor, "pour", PourAmount.Things("dram") + " of " + GetLiquidName() + " out", FromDialog: true);
                        }
                        if (!PourIntoCell(Actor, TargetCell, PourAmount, ref RequestInterfaceExit, CanPourOn: !Forced))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    Actor?.Fail("There's nothing in " + ParentObject.t() + " to pour.");
                }
            }
            return true;
        }

        public bool Pour(
            GameObject Actor = null,
            Cell TargetCell = null,
            bool Forced = false,
            bool Douse = false,
            int PourAmount = -1,
            bool OwnershipHandled = false
        )
        {
            bool RequestInterfaceExit = false;
            return Pour(ref RequestInterfaceExit, Actor, TargetCell, Forced, Douse, PourAmount, OwnershipHandled);
        }

        public double GetLiquidWeight()
        {
            return Volume * GetLiquidWeightPerDram();
        }

        public void SmearOn(GameObject Object, GameObject By, bool FromCell)
        {
            switch (ComponentLiquids.Count)
            {
                case 0:
                    break;
                case 1:
                    if (Primary == null)
                    {
                        string liquid = null;
                        foreach (string k in ComponentLiquids.Keys)
                        {
                            liquid = k;
                        }
                        if (liquid != null)
                        {
                            GetLiquid(liquid).SmearOn(this, Object, By, FromCell);
                        }
                    }
                    else
                    {
                        RequirePrimaryLiquid().SmearOn(this, Object, By, FromCell);
                    }
                    break;
                default:
                {
                    List<string> keylist = getKeylist();
                    try
                    {
                        keylist.Clear();
                        foreach (KeyValuePair<string, int> pair in ComponentLiquids)
                        {
                            keylist.Add(pair.Key);
                        }
                        foreach (string liquid in keylist)
                        {
                            GetLiquid(liquid).SmearOn(this, Object, By, FromCell);
                        }
                    }
                    finally
                    {
                        keylistPool.Enqueue(keylist);
                    }
                    break;
                }
            }
            ProcessExposure(Object, By, FromCell);
        }

        public void SmearOnTick(GameObject Object, GameObject By, bool FromCell)
        {
            switch (ComponentLiquids.Count)
            {
                case 0:
                    break;
                case 1:
                    if (Primary == null)
                    {
                        string liquid = null;
                        foreach (string k in ComponentLiquids.Keys)
                        {
                            liquid = k;
                        }
                        if (liquid != null)
                        {
                            GetLiquid(liquid).SmearOnTick(this, Object, By, FromCell);
                        }
                    }
                    else
                    {
                        RequirePrimaryLiquid().SmearOnTick(this, Object, By, FromCell);
                    }
                    break;
                default:
                {
                    List<string> keylist = getKeylist();
                    try
                    {
                        keylist.Clear();
                        foreach (KeyValuePair<string, int> pair in ComponentLiquids)
                        {
                            keylist.Add(pair.Key);
                        }
                        foreach (string liquid in keylist)
                        {
                            GetLiquid(liquid).SmearOnTick(this, Object, By, FromCell);
                        }
                    }
                    finally
                    {
                        keylistPool.Enqueue(keylist);
                    }
                    break;
                }
            }
            ProcessExposure(Object, By, FromCell);
        }

        public bool CanEvaporate()
        {
            if (!IsOpenVolume())
            {
                return false;
            }
            if (IsWadingDepth())
            {
                return false;
            }
            // this is a completely nonsensical condition that exists solely
            // so "treasure" pools on the ground don't disappear (pools of
            // pure water and cloning draught mainly); if we find a better
            // way to accomplish that we should let pure liquids evaporate
            if (ComponentLiquids.Count == 1)
            {
                return false;
            }
            return true;
        }

        public bool CheckGroundLiquidMerge()
        {
            if (Volume <= 0)
            {
                return false;
            }
            if (!IsOpenVolume())
            {
                return false;
            }
            if (IsWadingDepth())
            {
                return false;
            }
            if (ParentObject == null)
            {
                return false;
            }
            Cell ourCell = ParentObject.CurrentCell;
            if (ourCell == null)
            {
                return false;
            }
            string groundLiquid = ourCell.GroundLiquid;
            if (groundLiquid.IsNullOrEmpty())
            {
                return false;
            }
            if (!IsPureLiquid(groundLiquid))
            {
                return false;
            }
            ParentObject.Obliterate();
            return true;
        }

        public bool PerformFill(
            GameObject Actor,
            ref bool RequestInterfaceExit,
            bool ownershipHandled = false
        )
        {
            if (EffectivelySealed())
            {
                return false;
            }
            if (ParentObject.IsInStasis())
            {
                Actor?.Fail("You cannot seem to interact with " + ParentObject.t() + " in any way.");
                return false;
            }
            if (Actor.IsPlayer() && ParentObject.Owner != null && !ownershipHandled)
            {
                if (Popup.ShowYesNoCancel(ParentObject.IndicativeDistal + ParentObject.Is + " not owned by you. Are you sure you want to take from " + ParentObject.them + "?") != DialogResult.Yes)
                {
                    return false;
                }
                else
                {
                    ParentObject.Physics.BroadcastForHelp(Actor);
                }
            }
            if (!Actor.CheckFrozen(Telekinetic: true))
            {
                return false;
            }
            List<GameObject> sameLiquid = Event.NewGameObjectList();
            List<GameObject> otherLiquid = Event.NewGameObjectList();
            List<GameObject> Containers = Event.NewGameObjectList();
            List<GameObject> inventory = Event.NewGameObjectList();
            Actor.GetInventoryAndEquipment(inventory);
            for (int i = 0, j = inventory.Count; i < j; i++)
            {
                GameObject obj = inventory[i];
                if (obj != ParentObject)
                {
                    LiquidVolume LV = obj.LiquidVolume;
                    if (LV != null && !LV.EffectivelySealed())
                    {
                        if (LV.LiquidSameAs(this))
                        {
                            sameLiquid.Add(obj);
                        }
                        else
                        {
                            otherLiquid.Add(obj);
                        }
                    }
                }
            }
            Containers.AddRange(sameLiquid);
            Containers.AddRange(otherLiquid);
            if (Containers.Count == 0)
            {
                Actor.Fail("You have no containers to fill.");
                return false;
            }
            GameObject Container = PickItem.ShowPicker(
                Items: Containers,
                Actor: Actor,
                PreserveOrder: true,
                Title: "[Select a container to fill]"
            );
            if (Container != null)
            {
                if (Container == ParentObject)
                {
                    Actor.Fail("You can't pour from a container into " + ParentObject.itself + ".");
                    return false;
                }
                try
                {
                    Container = Container.RemoveOne();
                    LiquidVolume pContainer = Container.LiquidVolume;
                    bool EmptyContainer = false;
                    if (pContainer.Volume > 0 && !pContainer.LiquidSameAs(this))
                    {
                        if (Actor.IsPlayer())
                        {
                            DialogResult Result = Popup.ShowYesNoCancel("Do you want to empty " + Container.t() + " first?");
                            if (Result == DialogResult.Cancel)
                            {
                                return false;
                            }
                            if (Result == DialogResult.Yes)
                            {
                                EmptyContainer = true;
                            }
                        }
                        else
                        {
                            EmptyContainer = true;
                        }
                    }
                    int PourableVolume = Math.Min(EmptyContainer ? pContainer.MaxVolume : pContainer.MaxVolume - pContainer.Volume, Volume);
                    int PourAmount = 0;
                    if (Actor.IsPlayer())
                    {
                        int? PourAmountSpec = Popup.AskNumber("How many drams? (max=" + PourableVolume + ")", Start: PourableVolume,Min: 0,Max: PourableVolume);
                        try
                        {
                            PourAmount = Convert.ToInt32(PourAmountSpec);
                        }
                        catch
                        {
                            return false;
                        }
                    }
                    else
                    {
                        PourAmount = PourableVolume;
                    }
                    if (PourAmount > Volume)
                    {
                        PourAmount = Volume;
                    }
                    if (PourAmount <= 0)
                    {
                        return false;
                    }
                    PlayWorldSound("Sounds/Interact/sfx_interact_liquidContainer_fill");
                    if (EmptyContainer)
                    {
                        pContainer.EmptyIntoCell(Pourer: Actor);
                    }
                    int MaxFillAmount = pContainer.MaxVolume - pContainer.Volume;
                    int FinalFillAmount = 0;
                    if (MaxFillAmount < Volume)
                    {
                        FinalFillAmount = MaxFillAmount;
                    }
                    else
                    {
                        FinalFillAmount = Volume;
                    }
                    if (FinalFillAmount > PourAmount)
                    {
                        FinalFillAmount = PourAmount;
                    }
                    pContainer.MixWith(
                        Liquid: this,
                        Amount: FinalFillAmount,
                        RequestInterfaceExit: ref RequestInterfaceExit,
                        PouredFrom: ParentObject,
                        PouredBy: Actor
                    );
                    CheckImage();
                    pContainer.CheckImage();
                }
                finally
                {
                    Container.CheckStack();
                }
            }
            return true;
        }

        public bool PerformFill(GameObject Actor, bool ownershipHandled = false)
        {
            bool RequestInterfaceExit = false;
            return PerformFill(Actor, ref RequestInterfaceExit, ownershipHandled);
        }

        private bool CanInteractWithAnything(Cell inCell = null)
        {
            if (inCell != null && IsOpenVolume() && inCell.HasBridge())
            {
                return false;
            }
            if (EffectivelySealed())
            {
                return false;
            }
            if (ParentObject.HasPart<FungalVision>() && FungalVisionary.VisionLevel <= 0)
            {
                return false;
            }

            return true;
        }

        private bool CanInteractWith(GameObject obj, Cell inCell = null, bool AnythingChecked = false)
        {
            if (!GameObject.Validate(ref obj))
            {
                return false;
            }
            if (obj == ParentObject)
            {
                return false;
            }
            if (!obj.IsReal)
            {
                return false;
            }
            if (obj.IsScenery)
            {
                return false;
            }
            if (obj.IsBridge)
            {
                return false;
            }
            if (!AnythingChecked && !CanInteractWithAnything(inCell))
            {
                return false;
            }
            if (!ParentObject.PhaseAndFlightMatches(obj))
            {
                return false;
            }
            if (obj.GetMatterPhase() >= MatterPhase.GAS)
            {
                return false;
            }
            return true;
        }

        public bool CanPaintWith(int Group)
        {
            var primary = GetPrimaryLiquid();
            if (primary == null) return false;
            return primary.GetPaintGroup(this) == Group;
        }

        [NonSerialized] private static StringBuilder PaintBuilder = new StringBuilder();
        [NonSerialized] public int LastPaintMask = -1;
        public void Paint(int Mask)
        {
            if (LastPaintMask == Mask) return;

            var primary = GetPrimaryLiquid();
            if (primary == null) return;

            if (Mask == 0 && !IsWadingDepth())
            {
                ParentObject.Render.Tile = primary.GetPuddle(this);
            }
            else
            {
                ParentObject.Render.Tile = PaintBuilder
                    .Clear()
                    .Append(primary.GetPaintAtlas(this))
                    .Append(primary.GetPaint(this))
                    .Append('-')
                    .AppendMask(Mask, 8)
                    .Append(primary.GetPaintExtension(this))
                    .ToString();
            }

            LastPaintMask = Mask;
        }

        public static string GetCleaningLiquidGeneralization()
        {
            string result = null;
            foreach (var liquid in Liquids.Values)
            {
                if (liquid.EnableCleaning)
                {
                    if (result == null)
                    {
                        result = liquid.GetName().Strip();
                    }
                    else
                    if (result.Contains(" or "))
                    {
                        result = null;
                        break;
                    }
                    else
                    {
                        string name = liquid.GetName().Strip();
                        if (result.CompareTo(name) > 0)
                        {
                            result = name + " or " + result;
                        }
                        else
                        {
                            result = result + " or " + name;
                        }
                    }
                }
            }
            return result ?? "liquid";
        }

        private class ComponentSorter : IComparer<KeyValuePair<string, int>>
        {
            public static readonly ComponentSorter Instance = new ComponentSorter();
            public int Compare(KeyValuePair<string, int> A, KeyValuePair<string, int> B)
            {
                return B.Value.CompareTo(A.Value);
            }
        }

    }

}
