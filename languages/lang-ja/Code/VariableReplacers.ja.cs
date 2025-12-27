using System.Collections.Generic;
using XRL.World;
using XRL.World.Text;
using XRL.World.Text.Attributes;
using XRL.World.Text.Delegates;
using XRL.Language;

// TODO: split out replacers into sub-files by argument and/or processing type
// e.g. VariableReplacers.List.cs, VariableReplacers.String.cs, VariableReplacers.Number.cs
namespace LanguagesOfQudJapanese
{
    [HasVariableReplacer(Lang = "ja")]
    public static class VariableReplacers
    {
        // AllowSecondPerson is false when we should use third-person for player,
        // like in player recipe names, player history
        private static bool UseSecondPerson(GameObject Object)
        {
            return Object.IsPlayer() && Grammar.AllowSecondPerson;
        }
        private static bool UseSecondPerson(GenderedNoun Noun)
        {
            return Noun.Pronouns.Name.Contains("player");
        }

        private static string GetDisplayNameOf(GameObject Object)
        {
            return Object.GetDisplayName(Short: true, BaseOnly: true);
        }


        //standard particles

        /// <summary>
        /// the は(wa) particle for topic
        /// </summary>
        [VariableReplacer("は")]
        [VariableExample("", "Player")]
        [VariableExample("メフメットは", "Mehmet")]
        public static string は(VariableContext Context, GameObject Object)
        {
            if (UseSecondPerson(Object))
            {
                // Japanese tends to drop 1st- & 2nd-person pronouns
                return "";
            }
            return GetDisplayNameOf(Object) + "は";
        }

        /// <summary>
        /// the が(ga) particle for subject
        /// </summary>
        [VariableReplacer("が")]
        [VariableExample("", "Player")]
        [VariableExample("メフメットが", "Mehmet")]
        public static string が(VariableContext Context, GameObject Object)
        {
            if (UseSecondPerson(Object))
            {
                // Japanese tends to drop 1st- & 2nd-person pronouns
                return "";
            }
            return GetDisplayNameOf(Object) + "が";
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
        public static string を(VariableContext Context, GameObject Object)
        {
            if (UseSecondPerson(Object))
            {
                // Japanese tends to drop 1st- & 2nd-person pronouns
                return "";
            }
            return GetDisplayNameOf(Object) + "を";
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
        public static string の(VariableContext Context, GameObject Object)
        {
            if (UseSecondPerson(Object))
            {
                // Japanese tends to drop 1st- & 2nd-person pronouns
                return "";
            }
            return GetDisplayNameOf(Object) + "の";
        }

        /// <summary>
        /// the と(to) particle for "with", "and" lists of nouns, quoting, etc.
        /// </summary>
        [VariableReplacer("と")]
        public static string と(VariableContext Context, GameObject Object)
        {
            if (UseSecondPerson(Object))
            {
                // Japanese tends to drop 1st- & 2nd-person pronouns
                return "";
            }
            return GetDisplayNameOf(Object) + "と";
        }

        /// <summary>
        /// the に(ni) particle for locations, destinations, "by" relations, etc.
        /// </summary>
        [VariableReplacer("に")]
        [VariableExample("", "Player")]
        [VariableExample("メフメットに", "Mehmet")]
        public static string に(VariableContext Context, GameObject Object)
        {
            if (UseSecondPerson(Object))
            {
                // Japanese tends to drop 1st- & 2nd-person pronouns
                return "";
            }
            return GetDisplayNameOf(Object) + "に";
        }

        /// <summary>
        /// the で(de) particle for location, means, etc.
        /// </summary>
        [VariableReplacer("で")]
        [VariableExample("", "Player")]
        [VariableExample("メフメットで", "Mehmet")]
        [VariableExample("松明で", "Torch")]
        public static string で(VariableContext Context, GameObject Object)
        {
            if (UseSecondPerson(Object))
            {
                // Japanese tends to drop 1st- & 2nd-person pronouns
                return "";
            }
            return GetDisplayNameOf(Object) + "で";
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
        public static string も(VariableContext Context, GameObject Object)
        {
            //も is for emphasis; its noun wouldn't be dropped
            if (UseSecondPerson(Object))
            {
                return "あなたも";
            }
            return GetDisplayNameOf(Object) + "も";
        }

        /// <summary>
        /// cases where it'd say あなた(anata) instead of dropping the 2nd-pers pronoun
        /// </summary>

        [VariableReplacer("あなたは")]
        [VariableExample("あなたは", "Player")]
        [VariableExample("メフメットは", "Mehmet")]
        public static string あなたは(VariableContext Context, GameObject Object)
        {
            if (UseSecondPerson(Object))
            {
                return "あなたは";
            }
            return GetDisplayNameOf(Object) + "は";
        }

        /// <summary>
        /// adding a comma 、after は
        /// </summary>
        [VariableReplacer("あなたは、")]
        [VariableExample("あなたは、", "Player")]
        [VariableExample("メフメットは、", "Mehmet")]
        public static string あなたはWithComma(VariableContext Context, GameObject Object)
        {
            if (UseSecondPerson(Object))
            {
                return "あなたは、";
            }
            return GetDisplayNameOf(Object) + "は、";
        }

        [VariableReplacer("あなたが")]
        [VariableExample("あなたが", "Player")]
        [VariableExample("メフメットが", "Mehmet")]
        public static string あなたが(VariableContext Context, GameObject Object)
        {
            if (UseSecondPerson(Object))
            {
                return "あなたが";
            }
            return GetDisplayNameOf(Object) + "が";
        }

        [VariableReplacer("あなたを")]
        public static string あなたを(VariableContext Context, GameObject Object)
        {
            if (UseSecondPerson(Object))
            {
                return "あなたを";
            }
            return GetDisplayNameOf(Object) + "を";
        }

        [VariableReplacer("あなたの")]
        public static string あなたの(VariableContext Context, GameObject Object)
        {
            if (UseSecondPerson(Object))
            {
                return "あなたの";
            }
            return GetDisplayNameOf(Object) + "の";
        }

        [VariableReplacer("あなたと")]
        public static string あなたと(VariableContext Context, GameObject Object)
        {
            if (UseSecondPerson(Object))
            {
                return "あなたと";
            }
            return GetDisplayNameOf(Object) + "と";
        }

        [VariableReplacer("あなたに")]
        public static string あなたに(VariableContext Context, GameObject Object)
        {
            if (UseSecondPerson(Object))
            {
                return "あなたに";
            }
            return GetDisplayNameOf(Object) + "に";
        }

        [VariableReplacer("あなたで")]
        public static string あなたで(VariableContext Context, GameObject Object)
        {
            if (UseSecondPerson(Object))
            {
                return "あなたで";
            }
            return GetDisplayNameOf(Object) + "で";
        }

        [VariableReplacer(Default = "どこか", Override = true)]
        public static string Direction(VariableContext Context, GameObject Object)
        {
            var result = Context.Default;
            var origin = XRL.The.Player;
            var destination = Object;

            if (origin != null && destination != null)
            {
                // TODO: process the short direction into Japanese
                result = origin.DescribeDirectionToward(destination, Short: true);
            }

            return result;
        }

        [VariableReplacer(Default = "どこか", Override = true)]
        public static string Direction(VariableContext Context, GameObject Origin, GameObject Object)
        {
            var result = Context.Default;
            // TODO: process the short direction into Japanese
            result = Origin.DescribeDirectionToward(Object, Short: true);

            return result;
        }

        /// <summary>
        /// 基数(kisuu, "cardinal number") - equivalent of "Cardinal" replacer 
        /// </summary>
        [VariableReplacer("基数", "cardinal", Override = true)]
        [VariableExample("零", 0)]
        [VariableExample("二十一", 21)]
        [VariableExample("マイナス二十一", -21)]
        [VariableExample("一千三百三十七", 1337)]
        [VariableExample("三万七千五百六十四", 37564)]
        public static void Cardinal(VariableContext Context, long Number)
        {
            Context.Value.Append(Translator.Cardinal(Number));
        }

        /// <summary>
        /// 助詞(joshi, "particle") - fills in with the parameter
        /// </summary>
        [VariableReplacer("助詞", Default = "が")]
        public static string Particle(VariableContext Context)
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
        [VariableExample("{{Y|甘くて}}{{g|便利で}}{{R|温かくて}}{{c|本当の}}{{M|すてきな}}", "{{Y|甘い}};;{{g|便利な}};;{{R|温かい}};;{{c|本当の}};;{{M|すてきな}}")]
        public static void てList(VariableContext Context, IReadOnlyList<string> Strings)
        {
            Context.Value.AppendてList(Strings);
        }

        //TODO: add a postprocessor StripRubyText to ReplacerBuilder
        //  e.g. "[届：\TODO]blah[thing\ruby] [b]leh blah" -> "届：blahthing [b]leh blah"
        //  probably should go in Qud code if we'll support ruby formatting generally

    }

}