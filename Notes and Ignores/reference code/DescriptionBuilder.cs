using ConsoleLib.Console;
using System.Collections.Generic;
using System.Text;
using System;
using XRL.Language;
using static XRL.Language.Strings;
using Cysharp.Text;

namespace XRL.World
{

    /// <summary>
    /// A fancy Dictionary of string keys (bits of a name) with int ordering (groups of types).
    /// Has many constants to make consitent ordering easier to reason.
    /// </summary>
    public partial class DescriptionBuilder : Dictionary<string, int>
    {

        public const int ORDER_BASE = 10;
        public const int ORDER_ADJECTIVE = -500;
        public const int ORDER_CLAUSE = 600;
        public const int ORDER_TAG = 1100;
        public const int ORDER_MARK = -800;

        public const int ORDER_ADJUST_EXTREMELY_EARLY = -60;
        public const int ORDER_ADJUST_VERY_EARLY = -40;
        public const int ORDER_ADJUST_EARLY = -20;
        public const int ORDER_ADJUST_SLIGHTLY_EARLY = -5;
        public const int ORDER_ADJUST_SLIGHTLY_LATE = 5;
        public const int ORDER_ADJUST_LATE = 20;
        public const int ORDER_ADJUST_VERY_LATE = 40;
        public const int ORDER_ADJUST_EXTREMELY_LATE = 60;

        public const int SHORT_CUTOFF = ORDER_TAG + ORDER_ADJUST_EXTREMELY_EARLY;

        public const int PRIORITY_VERY_LOW = 5;
        public const int PRIORITY_LOW = 10;
        public const int PRIORITY_MEDIUM = 20;
        public const int PRIORITY_HIGH = 30;
        public const int PRIORITY_OVERRIDE = 40;

        public const int PRIORITY_ADJUST_SMALL = 1;
        public const int PRIORITY_ADJUST_MEDIUM = 2;
        public const int PRIORITY_ADJUST_LARGE = 3;

        public GameObject Object;
        public int Cutoff = int.MaxValue;
        public string PrimaryBase;
        public string LastAdded;
        public string Color;
        public int ColorPriority;
        public string SizeAdjective;
        public int SizeAdjectivePriority;
        public int SizeAdjectiveOrderAdjust;
        public List<string> Epithets;
        public Dictionary<string, int> EpithetOrder;
        public List<string> Titles;
        public Dictionary<string, int> TitleOrder;
        public List<string> WithClauses;
        public Dictionary<string, int> WithClauseOrder;
        public bool BaseOnly;

        [Obsolete("Use DescriptionBuilder.Create() - Supports Translator interface")]
        public DescriptionBuilder()
        {
        }

        [Obsolete("Use DescriptionBuilder.Create() - Supports Translator interface")]
        public DescriptionBuilder(int Cutoff) : this()
        {
            this.Cutoff = Cutoff;
        }

        [Obsolete("Use DescriptionBuilder.Create() - Supports Translator interface")]
        public DescriptionBuilder(int Cutoff, bool BaseOnly) : this(Cutoff)
        {
            this.BaseOnly = BaseOnly;
        }

        /// <summary>
        /// Creates a DescriptionBuilder using Translator.CreateDescriptionBuilder
        /// </summary>
        public static DescriptionBuilder Create(int Cutoff = int.MaxValue, bool BaseOnly = false)
        {
            return Translator.CreateDescriptionBuilder(Cutoff, BaseOnly);
        }

        new public virtual void Add(string Desc, int Order = 0)
        {
            if (Order < Cutoff)
            {
                if (this.ContainsKey(Desc))
                {
                    if (this[Desc] < Order)
                    {
                        this[Desc] = Order;
                        LastAdded = Desc;
                    }
                }
                else
                {
                    base.Add(Desc, Order);
                    LastAdded = Desc;
                }
            }
        }

        new public virtual void Remove(string Desc)
        {
            base.Remove(Desc);
            if (Desc == PrimaryBase)
            {
                PrimaryBase = null;
            }
        }

        new public virtual void Clear()
        {
            base.Clear();
            PrimaryBase = null;
            LastAdded = null;
            Color = null;
            SizeAdjective = null;
            Epithets?.Clear();
            EpithetOrder?.Clear();
            Titles?.Clear();
            TitleOrder?.Clear();
            WithClauses?.Clear();
            WithClauseOrder?.Clear();
        }

        public virtual void AddBase(string Base, int OrderAdjust = 0, bool Secondary = false)
        {
            Add(Base, ORDER_BASE + OrderAdjust);
            if (PrimaryBase == null && !Secondary)
            {
                PrimaryBase = Base;
            }
        }

        public virtual void ReplacePrimaryBase(string Base, int OrderAdjust = 0)
        {
            if (PrimaryBase != null)
            {
                Remove(PrimaryBase);
            }
            Add(Base, ORDER_BASE + OrderAdjust);
            PrimaryBase = Base;
        }

        public virtual void AddAdjective(string Adjective, int OrderAdjust = 0)
        {
            if (!BaseOnly)
            {
                Add(Adjective, ORDER_ADJECTIVE + OrderAdjust);
            }
        }

        public virtual void ApplySizeAdjective(
            string Adjective,
            int Priority = DescriptionBuilder.PRIORITY_LOW,
            int OrderAdjust = 0
        )
        {
            if (SizeAdjective == null || Priority > SizeAdjectivePriority)
            {
                SizeAdjective = Adjective;
                SizeAdjectivePriority = Priority;
                SizeAdjectiveOrderAdjust = OrderAdjust;
            }
        }

        public virtual void AddClause(string Clause, int OrderAdjust = 0)
        {
            if (!BaseOnly)
            {
                Add(Clause, ORDER_CLAUSE + OrderAdjust);
            }
        }

        public virtual void AddHonorific(string Honorific, int OrderAdjust = 0)
        {
            if (!BaseOnly)
            {
                AddAdjective(Honorific, ORDER_ADJUST_EXTREMELY_LATE);
            }
        }

        public virtual void AddEpithet(string Epithet, int OrderAdjust = 0)
        {
            if (!BaseOnly)
            {
                Epithets ??= new();
                Epithets.Add(Epithet);
                if (OrderAdjust != 0)
                {
                    EpithetOrder ??= new();
                    EpithetOrder[Epithet] = OrderAdjust;
                }
            }
        }

        public virtual void AddTitle(string Title, int OrderAdjust = 0)
        {
            if (!BaseOnly)
            {
                Titles ??= new();
                Titles.Add(Title);
                if (OrderAdjust != 0)
                {
                    TitleOrder ??= new();
                    TitleOrder[Title] = OrderAdjust;
                }
            }
        }

        public virtual void AddWithClause(string Clause, int OrderAdjust = 0)
        {
            if (!BaseOnly)
            {
                WithClauses ??= new();
                WithClauses.Add(Clause);
                if (OrderAdjust != 0)
                {
                    WithClauseOrder ??= new();
                    WithClauseOrder[Clause] = OrderAdjust;
                }
            }
        }

        public virtual void AddTag(string Tag, int OrderAdjust = 0)
        {
            if (!BaseOnly)
            {
                Add(Tag, ORDER_TAG + OrderAdjust);
            }
        }

        public virtual void AddMark(string Mark, int OrderAdjust = 0)
        {
            if (!BaseOnly)
            {
                Add(Mark, ORDER_MARK + OrderAdjust);
            }
        }

        public virtual void AddColor(string Color, int Priority = 0)
        {
            if (Priority >= ColorPriority)
            {
                this.Color = Color;
                this.ColorPriority = Priority;
            }
        }

        public virtual void AddColor(char Color, int Priority = 0)
        {
            if (Priority >= ColorPriority)
            {
                this.Color = "" + Color;
                this.ColorPriority = Priority;
            }
        }

        public virtual void Reset()
        {
            Clear();
            Object = null;
            Cutoff = int.MaxValue;
            Color = null;
            ColorPriority = int.MinValue;
            SizeAdjectivePriority = int.MinValue;
            SizeAdjectiveOrderAdjust = 0;
            BaseOnly = false;
        }

        protected virtual int SortEpithets(string A, string B)
        {
            if (EpithetOrder != null && EpithetOrder.Count > 0)
            {
                EpithetOrder.TryGetValue(A, out int aOrder);
                EpithetOrder.TryGetValue(B, out int bOrder);
                int orderCompare = aOrder.CompareTo(bOrder);
                if (orderCompare != 0)
                {
                    return orderCompare;
                }
            }
            return ColorUtility.CompareExceptFormattingAndCase(A, B);
        }

        protected virtual int SortTitles(string A, string B)
        {
            if (TitleOrder != null && TitleOrder.Count > 0)
            {
                TitleOrder.TryGetValue(A, out int aOrder);
                TitleOrder.TryGetValue(B, out int bOrder);
                int orderCompare = aOrder.CompareTo(bOrder);
                if (orderCompare != 0)
                {
                    return orderCompare;
                }
            }
            return ColorUtility.CompareExceptFormattingAndCase(A, B);
        }

        protected virtual int SortWithClauses(string A, string B)
        {
            if (WithClauseOrder != null && WithClauseOrder.Count > 0)
            {
                WithClauseOrder.TryGetValue(A, out int aOrder);
                WithClauseOrder.TryGetValue(B, out int bOrder);
                int orderCompare = aOrder.CompareTo(bOrder);
                if (orderCompare != 0)
                {
                    return orderCompare;
                }
            }
            return ColorUtility.CompareExceptFormattingAndCase(A, B);
        }

        protected virtual void ResolveEpithets()
        {
            if (Epithets != null && Epithets.Count > 0)
            {
                string clause;
                if (Epithets.Count > 1)
                {
                    Epithets.Sort(SortEpithets);
                    clause = _T("DescriptionBuilder Epithets Join", "=epithets.join:, =")
                        .AddArgument(Epithets, "epithets")
                        .ToString();
                }
                else
                {
                    clause = Epithets[0];
                }
                AddBase(clause, ORDER_ADJUST_EXTREMELY_LATE);
                Epithets.Clear();
                EpithetOrder?.Clear();
            }
        }

        protected virtual void ResolveTitles()
        {
            if (Titles != null && Titles.Count > 0)
            {
                string clause;
                if (Titles.Count > 1)
                {
                    Titles.Sort(SortTitles);
                    clause = _T("DescriptionBuilder Multiple Titles Clause", ", =titles.andList:noSerial=")
                        .AddArgument(Titles, "titles")
                        .ToString();
                }
                else
                if ((Epithets != null && Epithets.Count > 0) || (Object != null && Object.HasProperName))
                {
                    clause = _T("DescriptionBuilder Single Title Clause With Epithets", ", =title.toString=")
                        .AddArgument(Titles[0], "title")
                        .ToString();
                }
                else
                {
                    clause = _T("DescriptionBuilder Single Title Clause", "and =title.toString=")
                        .AddArgument(Titles[0], "title")
                        .ToString();
                }
                AddClause(clause, ORDER_ADJUST_EXTREMELY_EARLY);
                Titles.Clear();
                TitleOrder?.Clear();
            }
        }

        protected virtual void ResolveWithClauses()
        {
            if (WithClauses != null && WithClauses.Count > 0)
            {
                if (WithClauses.Count > 1)
                {
                    WithClauses.Sort(SortWithClauses);
                }
                var clause = _T("DescriptionBuilder Multiple With Clause", "with =withClauses.andList=")
                    .AddArgument(WithClauses, "withClauses")
                    .ToString();
                AddClause(clause);
                WithClauses.Clear();
            }
        }

        public virtual void Resolve()
        {
            if (!SizeAdjective.IsNullOrEmpty())
            {
                AddAdjective(SizeAdjective, SizeAdjectiveOrderAdjust);
                SizeAdjective = null;
                SizeAdjectivePriority = int.MinValue;
                SizeAdjectiveOrderAdjust = 0;
            }
            ResolveEpithets();
            ResolveTitles();
            ResolveWithClauses();
        }

        private static List<string> Descs = new();

        protected virtual int descOrderComparison(string a, string b)
        {
            int orderComparison = this[a].CompareTo(this[b]);
            if (orderComparison != 0)
            {
                return orderComparison;
            }
            return ColorUtility.CompareExceptFormattingAndCase(a, b);
        }

        protected virtual string ToStringCount1()
        {
            if (Color.IsNullOrEmpty())
            {
                return LastAdded;
            }
            using var SB = ZString.CreateStringBuilder();
            SB.Append("{{");
            SB.Append(Color);
            SB.Append('|');
            SB.Append(LastAdded);
            SB.Append("}}");
            return SB.ToString();
        }

        protected virtual bool ShouldAddSpaceBetween(ReadOnlySpan<char> Building, string Adding)
        {
            return Adding.Length < 1 || (Adding[0] != ':' && Adding[0] != ',' && Adding[0] != '-');
        }

        protected virtual string ToStringCountMany()
        {
            using var SB = ZString.CreateStringBuilder();

            Descs.Clear();
            Descs.AddRange(this.Keys);
            if (Descs.Count > 1)
            {
                Descs.Sort(descOrderComparison);
            }

            bool needColorClose = false;
            if (!Color.IsNullOrEmpty())
            {
                SB.Append("{{");
                SB.Append(Color);
                SB.Append('|');
                needColorClose = true;
            }
            for (int i = 0, j = Descs.Count; i < j; i++)
            {
                string desc = Descs[i];
                if (i > 0)
                {
                    if (needColorClose && this[desc] > ORDER_CLAUSE)
                    {
                        SB.Append("}}");
                        needColorClose = false;
                    }
                    if (ShouldAddSpaceBetween(SB.AsSpan(), desc))
                    {
                        SB.Append(' ');
                    }
                }
                SB.Append(desc);
            }
            if (needColorClose)
            {
                SB.Append("}}");
                needColorClose = false;
            }
            return SB.ToString();
        }

        public override string ToString()
        {
            Resolve();
            if (Count == 0)
            {
                return "";
            }
            if (Count == 1)
            {
                return ToStringCount1();
            }
            return ToStringCountMany();
        }

        public virtual string GetDebugInfo()
        {
            using var SB = ZString.CreateStringBuilder();
            bool firstItem = true;
            foreach (KeyValuePair<string, int> KV in this)
            {
                if (firstItem)
                {
                    firstItem = false;
                }
                else
                {
                    SB.Append(',');
                }
                SB.Append(KV.Key);
                SB.Append(':');
                SB.Append(KV.Value);
            }
            SB.Append(";Cutoff:");
            SB.Append(Cutoff);
            SB.Append(";PrimaryBase:");
            SB.Append(PrimaryBase);
            SB.Append(";LastAdded:");
            SB.Append(LastAdded);
            SB.Append(";Color:");
            SB.Append(Color);
            SB.Append(";ColorPriority:");
            SB.Append(ColorPriority);
            SB.Append(";SizeAdjective:");
            SB.Append(SizeAdjective);
            SB.Append(";SizeAdjectivePriority:");
            SB.Append(SizeAdjectivePriority);
            SB.Append(";SizeAdjectiveOrderAdjust:");
            SB.Append(SizeAdjectiveOrderAdjust);
            SB.Append(";BaseOnly:");
            SB.Append(BaseOnly);

            if (Epithets != null && Epithets.Count > 0)
            {
                SB.Append(";Epithets:");
                bool firstEpithet = true;
                foreach (string title in Epithets)
                {
                    if (firstEpithet)
                    {
                        firstEpithet = false;
                    }
                    else
                    {
                        SB.Append(',');
                    }
                    SB.Append(title);
                }
            }
            if (Titles != null && Titles.Count > 0)
            {
                SB.Append(";Titles:");
                bool firstTitle = true;
                foreach (string title in Titles)
                {
                    if (firstTitle)
                    {
                        firstTitle = false;
                    }
                    else
                    {
                        SB.Append(',');
                    }
                    SB.Append(title);
                }
            }
            if (WithClauses != null && WithClauses.Count > 0)
            {
                SB.Append(";WithClauses:");
                bool firstWith = true;
                foreach (string clause in WithClauses)
                {
                    if (firstWith)
                    {
                        firstWith = false;
                    }
                    else
                    {
                        SB.Append(',');
                    }
                    SB.Append(clause);
                }
            }
            return SB.ToString();
        }

    }

}
