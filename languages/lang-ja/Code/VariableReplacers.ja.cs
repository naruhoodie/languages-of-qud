using XRL.World;
using XRL.World.Text;
using XRL.World.Text.Attributes;
using XRL.World.Text.Delegates;
using XRL.Language;
using System.Collections.Generic;

// TODO: split out replacers into sub-files by argument and/or processing type
// e.g. VariableReplacers.List.cs, VariableReplacers.String.cs, VariableReplacers.Number.cs
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
        [VariableReplacer("は")]
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
        /// the が(ga) particle for subject
        /// </summary>
        [VariableReplacer("が")]
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
        /// 
        /// Phrase may contain a ⁂ as an infix location for the particle,
        /// otherwise it goes at the end.
        /// </summary>
        [VariableReplacer("が")]
        public static string が(VariableContext Context, string Phrase)
        {
            if (Phrase.Contains("⁂"))
            {
                return Phrase.Replace("⁂", "が");
            }
            return Phrase + "が";
        }

        /// <summary>
        /// the を(wo) particle for direct objects
        /// </summary>
        [VariableReplacer("を")]
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
        /// 
        /// Phrase may contain a ⁂ as an infix location for the particle,
        /// otherwise it goes at the end.
        /// </summary>
        [VariableReplacer("を")]
        public static string を(VariableContext Context, string Phrase)
        {
            if (Phrase.Contains("⁂"))
            {
                return Phrase.Replace("⁂", "を");
            }
            return Phrase + "を";
        }

        /// <summary>
        /// the の(no) particle for possessive/"of" relations, etc.
        /// </summary>
        [VariableReplacer("の")]
        [VariableExample("", "Player")]
        [VariableExample("メフメットの", "Mehmet")]
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
        /// the と(to) particle for "with", "and" lists of nouns, quoting, etc.
        /// </summary>
        [VariableReplacer("と")]
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
        /// the に(ni) particle for locations, destinations, "by" relations, etc.
        /// </summary>
        [VariableReplacer("に")]
        [VariableExample("", "Player")]
        [VariableExample("メフメットに", "Mehmet")]
        public static string に(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                // Japanese tends to drop 1st- & 2nd-person pronouns
                return "";
            }
            return GetDisplayNameOf(target) + "に";
        }

        /// <summary>
        /// the で(de) particle for location, means, etc.
        /// </summary>
        [VariableReplacer("で")]
        [VariableExample("", "Player")]
        [VariableExample("メフメットで", "Mehmet")]
        [VariableExample("松明で", "Torch")]
        public static string で(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                // Japanese tends to drop 1st- & 2nd-person pronouns
                return "";
            }
            return GetDisplayNameOf(target) + "で";
        }

        /// <summary>
        /// the で(de) particle for location, means, etc.
        /// 
        /// Phrase may contain a ⁂ as an infix location for the particle,
        /// otherwise it goes at the end.
        /// </summary>
        [VariableReplacer("で")]
        public static string で(VariableContext Context, string Phrase)
        {
            if (Phrase.Contains("⁂"))
            {
                return Phrase.Replace("⁂", "で");
            }
            return Phrase + "で";
        }

        /// <summary>
        /// the も(mo) particle for "too", "also", "even"
        /// </summary>
        [VariableReplacer("も")]
        [VariableExample("あなたも", "Player")]
        [VariableExample("メフメットも", "Mehmet")]
        public static string も(VariableContext Context, GameObject target)
        {
            //も is for emphasis; its noun wouldn't be dropped
            if (target.IsPlayer())
            {
                return "あなたも";
            }
            return GetDisplayNameOf(target) + "も";
        }

        /// <summary>
        /// cases where it'd say あなた(anata) instead of dropping the 2nd-pers pronoun
        /// </summary>

        [VariableReplacer("あなたは")]
        [VariableExample("あなたは", "Player")]
        [VariableExample("メフメットは", "Mehmet")]
        public static string あなたは(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                return "あなたは";
            }
            return GetDisplayNameOf(target) + "は";
        }

        /// <summary>
        /// adding a comma 、after は
        /// </summary>
        [VariableReplacer("あなたは、")]
        [VariableExample("あなたは、", "Player")]
        [VariableExample("メフメットは、", "Mehmet")]
        public static string あなたはWithComma(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                return "あなたは、";
            }
            return GetDisplayNameOf(target) + "は、";
        }

        [VariableReplacer("あなたが")]
        [VariableExample("あなたが", "Player")]
        [VariableExample("メフメットが", "Mehmet")]
        public static string あなたが(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                return "あなたが";
            }
            return GetDisplayNameOf(target) + "が";
        }

        [VariableReplacer("あなたを")]
        public static string あなたを(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                return "あなたを";
            }
            return GetDisplayNameOf(target) + "を";
        }

        [VariableReplacer("あなたの")]
        public static string あなたの(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                return "あなたの";
            }
            return GetDisplayNameOf(target) + "の";
        }

        [VariableReplacer("あなたと")]
        public static string あなたと(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                return "あなたと";
            }
            return GetDisplayNameOf(target) + "と";
        }

        [VariableReplacer("あなたに")]
        public static string あなたに(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                return "あなたに";
            }
            return GetDisplayNameOf(target) + "に";
        }

        [VariableReplacer("あなたで")]
        public static string あなたで(VariableContext Context, GameObject target)
        {
            if (target.IsPlayer())
            {
                return "あなたで";
            }
            return GetDisplayNameOf(target) + "で";
        }

        [VariableReplacer(Default = "どこか", Override = true)]
        public static string direction(VariableContext Context, GameObject Target)
        {
            var result = Context.Default;
            var origin = XRL.The.Player;
            var destination = Target;

            if (origin != null && destination != null)
            {
                // TODO: process the short direction into Japanese
                result = origin.DescribeDirectionToward(destination, Short: true);
            }

            return result;
        }

        [VariableReplacer(Default = "どこか", Override = true)]
        public static string direction(VariableContext Context, GameObject Origin, GameObject Target)
        {
            var result = Context.Default;
            // TODO: process the short direction into Japanese
            result = Origin.DescribeDirectionToward(Target, Short: true);

            return result;
        }

        /// <summary>
        /// 基数(kisuu, "cardinal number") - equivalent of "Cardinal" replacer 
        /// </summary>
        [VariableReplacer("基数")]
        [VariableExample("零", 0)]
        [VariableExample("二十一", 21)]
        [VariableExample("マイナス二十一", -21)]
        [VariableExample("一千三百三十七", 1337)]
        public static string 基数(VariableContext Context, int Number)
        {
            return TranslatorJapanese.Cardinal(Number);
        }

        /// <summary>
        /// 助詞(joshi, "particle") - fills in with the parameter 
        /// </summary>
        [VariableReplacer("助詞", Default = "が")]
        public static string 助詞(VariableContext Context)
        {
            return Context.Parameters.Count > 0 ? Context.Parameters[0] : Context.Default;
        }

        /// <summary>
        /// Join together a list of "adjective" strings by converting to て-form.
        /// な-adjectives: replace with で
        /// い-adjectives: replace with くて
        /// の-adjectives: no change
        /// </summary>
        [VariableReplacer("てList")]
        [VariableExample("甘くて便利で温かくて本当のすてきな", "甘い;;便利な;;温かい;;本当の;;すてきな")]
        public static string てList(VariableContext Context, IReadOnlyList<string> Strings)
        {
            return TranslatorJapanese.MakeてList(Strings);
        }

        //TODO: add a postprocessor StripRubyText to ReplacerBuilder
        //  e.g. "[届：\TODO]blah[thing\ruby] [b]leh blah" -> "届：blahthing [b]leh blah"
        //  probably should go in Qud code if we'll support ruby formatting generally

    }

}