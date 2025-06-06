using System;
using XRL.World;

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
    }
}