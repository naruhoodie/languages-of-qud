using XRL.World;
using XRL.World.Text.Attributes;
using XRL.World.Text.Delegates;

namespace LanguagesOfQud
{
    [HasVariableReplacer(Lang = "ja")]
    public static class VariableReplacers
    {
        //standard particles

        [VariableObjectReplacer("を")]
        public static string をReplacer(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    // Japanese tends to drop 1st- & 2nd-person pronouns
                    return "";
                }
                return target.GetDisplayName() + "を";
            }
            return Context.Explicit + "を";
        }

        [VariableObjectReplacer("が")]
        public static string がReplacer(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    // Japanese tends to drop 1st- & 2nd-person pronouns
                    return "";
                }
                return target.GetDisplayName() + "が";
            }
            return Context.Explicit + "が";
        }

        [VariableObjectReplacer("の")]
        public static string のReplacer(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    // Japanese tends to drop 1st- & 2nd-person pronouns
                    return "";
                }
                return target.GetDisplayName() + "の";
            }
            return Context.Explicit + "の";
        }

        [VariableObjectReplacer("と")]
        public static string とReplacer(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    // Japanese tends to drop 1st- & 2nd-person pronouns
                    return "";
                }
                return target.GetDisplayName() + "と";
            }
            return Context.Explicit + "と";
        }

        [VariableObjectReplacer("に")]
        public static string にReplacer(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    // Japanese tends to drop 1st- & 2nd-person pronouns
                    return "";
                }
                return target.GetDisplayName() + "に";
            }
            return Context.Explicit + "に";
        }

        // cases where it'd say あなた instead of dropping a 2nd-person pronoun

        [VariableObjectReplacer("あなたを")]
        public static string あなたをReplacer(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    return "あなたを";
                }
                return target.GetDisplayName() + "を";
            }
            return Context.Explicit + "を";
        }

        [VariableObjectReplacer("あなたが")]
        public static string あなたがReplacer(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    return "あなたが";
                }
                return target.GetDisplayName() + "が";
            }
            return Context.Explicit + "が";
        }

        [VariableObjectReplacer("あなたの")]
        public static string あなたのReplacer(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    return "あなたの";
                }
                return target.GetDisplayName() + "の";
            }
            return Context.Explicit + "の";
        }

        [VariableObjectReplacer("あなたと")]
        public static string あなたとReplacer(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    return "あなたと";
                }
                return target.GetDisplayName() + "と";
            }
            return Context.Explicit + "と";
        }

        [VariableObjectReplacer("あなたに")]
        public static string あなたにReplacer(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    return "あなたに";
                }
                return target.GetDisplayName() + "に";
            }
            return Context.Explicit + "に";
        }

        [VariableObjectReplacer(Default = "どこか")]
        public static string Direction(DelegateContext Context)
        {
            var result = Context.Default;
            var origin = XRL.The.Player;
            var destination = Context.Target;
            if (Context.Arguments.Count >= 2)
            {
                origin = Context.Target;
                destination = Context.Arguments[1].Object;
            }

            if (origin != null && destination != null)
            {
                // TODO: process the short direction into Japanese
                result = origin.DescribeDirectionToward(destination, Short: true);
            }

            return result;
        }

        //TODO: add a postprocessor StripRubyText to ReplacerBuilder
        //  e.g. "[届：\TODO]blah[thing\ruby] [b]leh blah" -> "届：blahthing [b]leh blah"
        //  probably should go in Qud code if we'll support ruby formatting generally
    }
}