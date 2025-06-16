using ConsoleLib.Console;
using System;
using XRL.World;
using static XRL.Language.Strings;

namespace XRL.Language
{
    /// <summary>
    /// used for building a complete DisplayName
    /// with descriptive elements ordered appropriately around the base name.
    /// 
    /// (Hero) creatures may have epithets and titles.
    /// Japanese-style ordering: [Title]、[FewShortEpithets][HeroName][Honorific]、[ManyEpithets]
    /// 
    /// Items could have mods (some of which are "with"-clauses e.g. "with suspensors")
    /// Japanese-style ordering: [Mark][WithClauses]を備えた、[Adjectives][ItemName]
    /// </summary>
    public class DescriptionBuilderJa : DescriptionBuilder
    {
        // const values from DescriptionBuilder, for reference

        //public const int ORDER_BASE = 10;
        //public const int ORDER_ADJECTIVE = -500;
        //public const int ORDER_CLAUSE = 600;
        //public const int ORDER_TAG = 1100;
        //public const int ORDER_MARK = -800;

        public DescriptionBuilderJa() { }

        protected override bool ShouldAddSpaceBetween(ReadOnlySpan<char> Building, string Adding)
        {
            // temporary return false just to prove the issue
            return false;
            // return Adding.Length < 1 || (Adding[0] != ':' && Adding[0] != ',' && Adding[0] != '-');
        }

        /// <summary>
        /// Epithets in Japanese go before the name
        /// if more than 3, do as a comma-separated list after the name
        /// </summary>
        protected override void ResolveEpithets()
        {
            if (Epithets != null && Epithets.Count > 0)
            {
                string clause;
                if (Epithets.Count > 3)
                {
                    Epithets.Sort(SortEpithets);
                    clause = _T("DescriptionBuilder Epithets Join", "=epithets.join:, =")
                        .AddArgument(Epithets, "epithets")
                        .ToString();
                    AddClause(clause, ORDER_ADJUST_VERY_EARLY);
                }
                else
                {
                    if (Epithets.Count > 1)
                    {
                    Epithets.Sort(SortEpithets);
                    clause = _T("DescriptionBuilder Epithets Few", "=epithets.てList=")
                        .AddArgument(Epithets, "epithets")
                        .ToString();
                    }
                    else
                    {
                        clause = Epithets[0];
                    }
                    AddBase(clause, ORDER_ADJUST_VERY_EARLY);
                }
                Epithets.Clear();
                EpithetOrder?.Clear();
            }
        }

        protected override void ResolveTitles()
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
                AddBase(clause, ORDER_ADJUST_EXTREMELY_EARLY);
                Titles.Clear();
                TitleOrder?.Clear();
            }
        }
    }
}