using ConsoleLib.Console;
using System;
using XRL.World;
using static XRL.Language.Strings;

namespace XRL.Language
{
    public class DescriptionBuilderJa : DescriptionBuilder
    {
        public DescriptionBuilderJa() { }
        protected override bool ShouldAddSpaceBetween(ReadOnlySpan<char> Building, string Adding)
        {
            // temporary return false just to prove the issue
            return false;
            // return Adding.Length < 1 || (Adding[0] != ':' && Adding[0] != ',' && Adding[0] != '-');
        }

        /// <summary>
        /// Epithets in Japanese go before the name
        /// They will be "adjectives" of some form
        /// for multiples, all but the last is converted to て-form
        /// </summary>
        protected override void ResolveEpithets()
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
                AddBase(clause, ORDER_ADJUST_VERY_EARLY);
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