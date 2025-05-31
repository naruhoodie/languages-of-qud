using XRL.World;
using XRL.World.Text;
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

        /// <summary>
        /// the は(wa) particle for topic
        /// </summary>
        [VariableReplacer("は", Capitalization = false)]
        [VariableExample("", "Player")]
        [VariableExample("メフメットは", "Mehmet")]
        public static string は(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                // Japanese tends to drop 1st- & 2nd-person pronouns
                return "";
            }
            return GetDisplayNameOf(target) + "は";
        }

        /// <summary>
        /// the は(wa) particle for topic
        /// </summary>
        [VariableReplacer("は", Capitalization = false)]
        public static string は(VariableContext Context, ExplicitObject Explicit)
        {
            return Explicit.Name + "は";
        }

        /// <summary>
        /// the が(ga) particle for subject
        /// </summary>
        [VariableReplacer("が", Capitalization = false)]
        [VariableExample("", "Player")]
        [VariableExample("メフメットが", "Mehmet")]
        public static string が(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                // Japanese tends to drop 1st- & 2nd-person pronouns
                return "";
            }
            return GetDisplayNameOf(target) + "が";
        }

        /// <summary>
        /// the が(ga) particle for subject
        /// </summary>
        [VariableReplacer("が", Capitalization = false)]

        public static string が(VariableContext Context, ExplicitObject target)
        {
            return target.Name + "が";
        }

        /// <summary>
        /// the を(wo) particle for direct objects
        /// </summary>
        [VariableReplacer("を", Capitalization = false)]
        [VariableExample("", "Player")]
        [VariableExample("メフメットを", "Mehmet")]
        public static string を(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                // Japanese tends to drop 1st- & 2nd-person pronouns
                return "";
            }
            return GetDisplayNameOf(target) + "を";
        }
        
        /// <summary>
        /// the を(wo) particle for direct objects
        /// </summary>
        [VariableReplacer("を", Capitalization = false)]
        public static string を(VariableContext Context, ExplicitObject Explicit)
        {
            return Explicit.Name + "を";
        }

        /// <summary>
        /// the の(no) particle for possessive/"of" relations
        /// </summary>
        [VariableReplacer("の", Capitalization = false)]
        public static string の(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                // Japanese tends to drop 1st- & 2nd-person pronouns
                return "";
            }
            return GetDisplayNameOf(target) + "の";
        }
        /// <summary>
        /// the の(no) particle for possessive/"of" relations
        /// </summary>
        [VariableReplacer("の", Capitalization = false)]
        public static string の(VariableContext Context, ExplicitObject Explicit)
        {
            return Explicit.Name + "の";
        }

        /// <summary>
        /// the と(to) particle
        /// </summary>
        [VariableReplacer("と", Capitalization = false)]
        public static string と(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                // Japanese tends to drop 1st- & 2nd-person pronouns
                return "";
            }
            return GetDisplayNameOf(target) + "と";
        }
        /// <summary>
        /// the と(to) particle
        /// </summary>
        [VariableReplacer("と", Capitalization = false)]
        public static string と(VariableContext Context, ExplicitObject Explicit)
        {
            return Explicit.Name + "と";
        }

        /// <summary>
        /// the に(ni) particle for locations, "by" relations, etc.
        /// </summary>
        [VariableReplacer("に", Capitalization = false)]
        public static string にReplacer(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                // Japanese tends to drop 1st- & 2nd-person pronouns
                return "";
            }
            return GetDisplayNameOf(target) + "に";
        }
        /// <summary>
        /// the に(ni) particle for locations, "by" relations, etc.
        /// </summary>
        [VariableReplacer("に", Capitalization = false)]
        public static string にReplacer(VariableContext Context, ExplicitObject Explicit)
        {
            return Explicit.Name + "に";
        }

        /// <summary>
        /// where it'd say あなた(anata) instead of dropping the 2nd-pers pronoun
        /// </summary>

        [VariableReplacer("あなたは", Capitalization = false)]
        public static string あなたは(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                return "あなたは";
            }
            return GetDisplayNameOf(target) + "は";
        }

        /// <summary>
        /// where it'd say あなた(anata) instead of dropping the 2nd-pers pronoun
        /// </summary>

        [VariableReplacer("あなたは", Capitalization = false)]
        public static string あなたは(VariableContext Context, ExplicitObject Explicit)
        {
            return Explicit.Name + "は";
        }

        /// <summary>
        /// adding a comma 、after は
        /// </summary>
        [VariableReplacer("あなたは、", Capitalization = false)]
        public static string あなたはWithComma(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                return "あなたは、";
            }
            return GetDisplayNameOf(target) + "は、";
        }
        /// <summary>
        /// adding a comma 、after は
        /// </summary>
        [VariableReplacer("あなたは、", Capitalization = false)]
        public static string あなたはWithComma(VariableContext Context, ExplicitObject Explicit)
        {
            return Explicit.Name + "は、";
        }

        [VariableReplacer("あなたが", Capitalization = false)]
        public static string あなたが(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                return "あなたが";
            }
            return GetDisplayNameOf(target) + "が";
        }
        [VariableReplacer("あなたが", Capitalization = false)]
        public static string あなたが(VariableContext Context, ExplicitObject Explicit)
        {
            return Explicit.Name + "が";
        }

        [VariableReplacer("あなたを", Capitalization = false)]
        public static string あなたを(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                return "あなたを";
            }
            return GetDisplayNameOf(target) + "を";
        }
        [VariableReplacer("あなたを", Capitalization = false)]
        public static string あなたを(VariableContext Context, ExplicitObject Explicit)
        {
            return Explicit.Name + "を";
        }

        [VariableReplacer("あなたの", Capitalization = false)]
        public static string あなたの(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                return "あなたの";
            }
            return GetDisplayNameOf(target) + "の";
        }
        [VariableReplacer("あなたの", Capitalization = false)]
        public static string あなたの(VariableContext Context, ExplicitObject Explicit)
        {
            return Explicit.Name + "の";
        }

        [VariableReplacer("あなたと", Capitalization = false)]
        public static string あなたと(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                return "あなたと";
            }
            return GetDisplayNameOf(target) + "と";
        }
        [VariableReplacer("あなたと", Capitalization = false)]
        public static string あなたと(VariableContext Context, ExplicitObject Explicit)
        {
            return Explicit.Name + "と";
        }

        [VariableReplacer("あなたに", Capitalization = false)]
        public static string あなたに(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                return "あなたに";
            }
            return GetDisplayNameOf(target) + "に";
        }
        [VariableReplacer("あなたに", Capitalization = false)]
        public static string あなたに(VariableContext Context, ExplicitObject Explicit)
        {
            return Explicit.Name + "に";
        }

        [VariableReplacer(Default = "どこか", Capitalization = false)]
        public static string Direction(VariableContext Context, GameObject Target)
        {
            var result = Context.Default;
            var origin = XRL.The.Player;
            var destination = Target;
            // if (Context.Arguments.Count >= 2)
            // {
            //     origin = Context.Target;
            //     destination = Context.Arguments[1].Object;
            // }

            if (origin != null && destination != null)
            {
                // TODO: process the short direction into Japanese
                result = origin.DescribeDirectionToward(destination, Short: true);
            }

            return result;
        }

        [VariableReplacer(Default = "どこか", Capitalization = false)]
        public static string Direction(VariableContext Context, GameObject Origin, GameObject Target)
        {
            var result = Context.Default;
            // TODO: process the short direction into Japanese
            result = Origin.DescribeDirectionToward(Target, Short: true);

            return result;
        }

        //TODO: add a postprocessor StripRubyText to ReplacerBuilder
        //  e.g. "[届：\TODO]blah[thing\ruby] [b]leh blah" -> "届：blahthing [b]leh blah"
        //  probably should go in Qud code if we'll support ruby formatting generally
    }
}