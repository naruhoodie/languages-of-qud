using XRL.World;
using XRL.World.Text.Attributes;
using XRL.World.Text.Delegates;

namespace LanguagesOfQud
{
    [HasVariableReplacer(Lang = "ja")]
    public static class VariableReplacers
    {
        private static string GetDisplayNameOf(GameObject GO)
        {
            return GO.GetDisplayName(Short: true, BaseOnly: true);
        }

        //AllowSecondPerson = false is for using third-person for player,
            // like in player recipe names, player history

            //standard particles

            // the は(wa) particle for topic
            [VariableObjectReplacer]
        public static string は(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    // Japanese tends to drop 1st- & 2nd-person pronouns
                    return "";
                }
                return GetDisplayNameOf(target) + "は";
            }
            return Context.Explicit + "は";
        }

        // the が(ga) particle for subject
        [VariableObjectReplacer]
        public static string が(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    // Japanese tends to drop 1st- & 2nd-person pronouns
                    return "";
                }
                return GetDisplayNameOf(target) + "が";
            }
            return Context.Explicit + "が";
        }

            // the を(wo) particle for direct objects
            [VariableObjectReplacer]
        public static string を(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    // Japanese tends to drop 1st- & 2nd-person pronouns
                    return "";
                }
                return GetDisplayNameOf(target) + "を";
            }
            return Context.Explicit + "を";
        }

        // the の(no) particle for possessive/"of" relations
        [VariableObjectReplacer]
        public static string の(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    // Japanese tends to drop 1st- & 2nd-person pronouns
                    return "";
                }
                return GetDisplayNameOf(target) + "の";
            }
            return Context.Explicit + "の";
        }

        // the と(to) particle
        [VariableObjectReplacer]
        public static string と(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    // Japanese tends to drop 1st- & 2nd-person pronouns
                    return "";
                }
                return GetDisplayNameOf(target) + "と";
            }
            return Context.Explicit + "と";
        }

        // the に(ni) particle for locations, "by" relations, etc.
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
                return GetDisplayNameOf(target) + "に";
            }
            return Context.Explicit + "に";
        }

        // where it'd say あなた(anata) instead of dropping the 2nd-pers pronoun

        [VariableObjectReplacer]
        public static string あなたは(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    return "あなたは";
                }
                return GetDisplayNameOf(target) + "は";
            }
            return Context.Explicit + "は";
        }

        // adding a comma 、after は
        [VariableObjectReplacer("あなたは、")]
        public static string あなたはWithComma(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    return "あなたは、";
                }
                return GetDisplayNameOf(target) + "は、";
            }
            return Context.Explicit + "は、";
        }

        [VariableObjectReplacer]
        public static string あなたが(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    return "あなたが";
                }
                return GetDisplayNameOf(target) + "が";
            }
            return Context.Explicit + "が";
        }

        [VariableObjectReplacer]
        public static string あなたを(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    return "あなたを";
                }
                return GetDisplayNameOf(target) + "を";
            }
            return Context.Explicit + "を";
        }

        [VariableObjectReplacer]
        public static string あなたの(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    return "あなたの";
                }
                return GetDisplayNameOf(target) + "の";
            }
            return Context.Explicit + "の";
        }

        [VariableObjectReplacer]
        public static string あなたと(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    return "あなたと";
                }
                return GetDisplayNameOf(target) + "と";
            }
            return Context.Explicit + "と";
        }

        [VariableObjectReplacer]
        public static string あなたに(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                if (target.IsPlayer())
                {
                    return "あなたに";
                }
                return GetDisplayNameOf(target) + "に";
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