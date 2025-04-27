using ConsoleLib.Console;
using HistoryKit;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System;
using XRL.World;

namespace XRL.Language
{
    public static class Grammar
    {

        public static bool AllowSecondPerson = true;

        public static string Pluralize(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return word;
            }
            if (word[0] == '=')
            {
                return "=pluralize=" + word;
            }

            string plural;
            if (singularToPlural.TryGetValue(word, out plural))
            {
                return plural;
            }

            if (irregularPluralization.TryGetValue(word, out plural))
            {
                return FoundPlural(word, plural);
            }

            Match match;
            match = Regex.Match(word, @"^{{(.*?)\|(.*)}}$");
            if (match.Success)
            {
                return FoundPlural(word, "{{" + match.Groups[1].Value + "|" + Pluralize(match.Groups[2].Value) + "}}");
            }
            match = Regex.Match(word, @"(.*?)(&.(?:\^.)?)$");
            if (match.Success)
            {
                return FoundPlural(word, Pluralize(match.Groups[1].Value) + match.Groups[2].Value);
            }
            match = Regex.Match(word, @"^(&.(?:\^.)?)(.*?)$");
            if (match.Success)
            {
                return FoundPlural(word, match.Groups[1].Value + Pluralize(match.Groups[2].Value));
            }
            match = Regex.Match(word, "^([*\\-_~'\"/])(.*)(\\1)$");
            if (match.Success)
            {
                return FoundPlural(word, match.Groups[1].Value + Pluralize(match.Groups[2].Value) + match.Groups[3].Value);
            }
            match = Regex.Match(word, "(.*?)( +)$");
            if (match.Success)
            {
                return FoundPlural(word, Pluralize(match.Groups[1].Value) + match.Groups[2].Value);
            }
            match = Regex.Match(word, "^( +)(.*?)$");
            if (match.Success)
            {
                return FoundPlural(word, match.Groups[1].Value + Pluralize(match.Groups[2].Value));
            }
            match = Regex.Match(word, @"^(.*)( \(.*\))$");
            if (match.Success)
            {
                return FoundPlural(word, Pluralize(match.Groups[1].Value) + match.Groups[2].Value);
            }
            match = Regex.Match(word, @"^(.*)( \[.*\])$");
            if (match.Success)
            {
                return FoundPlural(word, Pluralize(match.Groups[1].Value) + match.Groups[2].Value);
            }
            match = Regex.Match(word, "^(.*)( (?:of|in a|in an|in the|into|for|from|o'|to|with) .*)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return FoundPlural(word, Pluralize(match.Groups[1].Value) + match.Groups[2].Value);
            }
            match = Regex.Match(word, @"^(.*)( (?:mk\.?|mark) *(?:[ivx]+))$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return FoundPlural(word, Pluralize(match.Groups[1].Value) + match.Groups[2].Value);
            }
            if (!word.Contains(" "))
            {
                match = Regex.Match(word, "^(.*)-(.*)$");
                if (match.Success)
                {
                    return FoundPlural(word, match.Groups[1].Value + "-" + Pluralize(match.Groups[2].Value));
                }
            }

            if (identicalPluralization.Contains(word))
            {
                return FoundPlural(word, word);
            }
            if (word.EndsWith("folk"))
            {
                return FoundPlural(word, word);
            }

            if (latinPluralization.Contains(word))
            {
                if (word.EndsWith("us"))
                {
                    return FoundPlural(word, word.Substring(0, word.Length - 2) + "i");
                }
                if (word.EndsWith("a"))
                {
                    return FoundPlural(word, word + "e");
                }
                if (word.EndsWith("num"))
                {
                    return FoundPlural(word, word.Substring(0, word.Length - 2) + "i");
                }
                if (word.EndsWith("um") || word.EndsWith("on"))
                {
                    return FoundPlural(word, word.Substring(0, word.Length - 2) + "a");
                }
                if (word.EndsWith("en"))
                {
                    return FoundPlural(word, word.Substring(0, word.Length - 2) + "ina");
                }
                if (word.EndsWith("is"))
                {
                    return FoundPlural(word, word.Substring(0, word.Length - 2) + "es");
                }
                if (word.EndsWith("es"))
                {
                    return FoundPlural(word, word.Substring(0, word.Length - 2) + "ites");
                }
                if (word.EndsWith("ex") || word.EndsWith("ix"))
                {
                    return FoundPlural(word, word.Substring(0, word.Length - 2) + "ices");
                }
                return FoundPlural(word, word);
            }

            if (greekPluralization1.Contains(word))
            {
                if (word.EndsWith("os") || word.EndsWith("on"))
                {
                    return FoundPlural(word, word.Substring(0, word.Length - 1) + "i");
                }
                if (word.EndsWith("is") || word.EndsWith("ix") || word.EndsWith("as"))
                {
                    return FoundPlural(word, word.Substring(0, word.Length - 2) + "des");
                }
                if (word.EndsWith("ys"))
                {
                    return FoundPlural(word, word.Substring(0, word.Length - 2) + "daes");
                }
                if (word.EndsWith("ma"))
                {
                    return FoundPlural(word, word + "ta");
                }
                if (word.EndsWith("a"))
                {
                    return FoundPlural(word, word + "e");
                }
                if (word.EndsWith("x"))
                {
                    return FoundPlural(word, word.Substring(0, word.Length - 2) + "ges");
                }
                if (word.EndsWith("or"))
                {
                    return FoundPlural(word, word + "es");
                }
                if (word.EndsWith("r"))
                {
                    return FoundPlural(word, word + "oi");
                }
                return FoundPlural(word, word + "a");
            }
            if (greekPluralization2.Contains(word))
            {
                if (word.EndsWith("on"))
                {
                    return FoundPlural(word, word.Substring(0, word.Length - 2) + "a");
                }
                if (word.EndsWith("is"))
                {
                    return FoundPlural(word, word.Substring(0, word.Length - 2) + "es");
                }
                return FoundPlural(word, word);
            }

            if (hebrewPluralization.Contains(word))
            {
                if (word.EndsWith("ah"))
                {
                    return FoundPlural(word, word.Substring(0, word.Length - 2) + "ot");
                }
                if (word.EndsWith("da"))
                {
                    return FoundPlural(word, word.Substring(0, word.Length - 1) + "ot");
                }
                if (word.EndsWith("esh"))
                {
                    return FoundPlural(word, word.Substring(0, word.Length - 3) + "ashot");
                }
                if (word.EndsWith("ch") || word.EndsWith("kh"))
                {
                    return FoundPlural(word, word + "ot");
                }
                if (word.EndsWith("a"))
                {
                    return FoundPlural(word, word.Substring(0, word.Length - 1) + "im");
                }
                return FoundPlural(word, word + "im");
            }

            if (word.Contains(" "))
            {
                string[] words = word.Split(' ');
                StringBuilder build = Event.NewStringBuilder();
                if (dualPluralization.Contains(words[words.Length - 1]))
                {
                    for (int i = 0; i < words.Length - 2; i++)
                    {
                        build.Append(words[i]);
                        build.Append(" ");
                    }
                    build.Append(Pluralize(words[words.Length - 2]));
                    build.Append(" ");
                    build.Append(Pluralize(words[words.Length - 1]));
                }
                else
                {
                    for (int i = 0; i < words.Length - 1; i++)
                    {
                        build.Append(words[i]);
                        build.Append(" ");
                    }
                    build.Append(Pluralize(words[words.Length - 1]));
                }
                return FoundPlural(word, build.ToString().Trim());
            }

            if (ColorUtility.HasUpperExceptFormatting(word))
            {
                if (ColorUtility.IsAllUpperExceptFormatting(word))
                {
                    return FoundPlural(word, ColorUtility.ToUpperExceptFormatting(Pluralize(ColorUtility.ToLowerExceptFormatting(word))));
                }
                if (ColorUtility.IsFirstUpperExceptFormatting(word))
                {
                    return FoundPlural(word, ColorUtility.CapitalizeExceptFormatting(Pluralize(ColorUtility.ToLowerExceptFormatting(word))));
                }
                return FoundPlural(word, Pluralize(ColorUtility.ToLowerExceptFormatting(word)));
            }

            if (word.EndsWith("elf") || word.EndsWith("olf") || word.EndsWith("arf") || word.EndsWith("alf"))
            {
                return FoundPlural(word, word.Substring(0, word.Length - 1) + "ves");
            }
            if (word.EndsWith("man") && !String.Equals(word, "human"))
            {
                return FoundPlural(word, word.Substring(0, word.Length - 2) + "en");
            }
            if (word.EndsWith("ife"))
            {
                return FoundPlural(word, word.Substring(0, word.Length - 2) + "ves");
            }
            if (word.EndsWith("mensch"))
            {
                return FoundPlural(word, word + "en");
            }

            if (word.Length == 1)
            {
                return FoundPlural(word, word + "s");
            }

            char last = word[word.Length - 1];
            char secLast = word[word.Length - 2];
            string lastTwo = word.Substring(word.Length - 2, 2);
            plural = word;
            if (last == 'z' && (secLast == 'a' || secLast == 'e' || secLast == 'i' || secLast == 'o' || secLast == 'u'))
            {
                plural += "z";
            }
            if (last == 's' || last == 'x' || last == 'z' || lastTwo == "sh" || lastTwo == "ss" || lastTwo == "ch" || (last == 'o' && secLast != 'o' && secLast != 'b'))
            {
                plural += "es";
            }
            else if (last == 'y' && secLast != 'a' && secLast != 'e' && secLast != 'i' && secLast != 'o' && secLast != 'u')
            {
                plural = plural.Substring(0, plural.Length - 1) + "ies";
            }
            else
            {
                plural += "s";
            }
            return FoundPlural(word, plural);
        }

        /**
         * support method for Pluralize()
         */
        private static string FoundPlural(string word, string plural)
        {
            if (!singularToPlural.ContainsKey(word))
            {
                singularToPlural.Add(word, plural);
            }
            if (!pluralToSingular.ContainsKey(plural))
            {
                pluralToSingular.Add(plural, word);
            }
            return plural;
        }

        public static string ThirdPerson(string word, bool PrependSpace = false)
        {
            // not prepending a space even if PrependSpace is true in this case is intentional
            if (string.IsNullOrEmpty(word))
            {
                return word;
            }

            string thirdPerson = "";
            if (PrependSpace)
            {
                if (firstPersonToThirdPersonWithSpace.TryGetValue(word, out thirdPerson))
                {
                    return thirdPerson;
                }
            }
            else
            {
                if (firstPersonToThirdPerson.TryGetValue(word, out thirdPerson))
                {
                    return thirdPerson;
                }
            }

            if (irregularThirdPerson.TryGetValue(word, out thirdPerson))
            {
                return FoundThirdPerson(word, thirdPerson, PrependSpace);
            }

            Match match;
            match = Regex.Match(word, @"(.*?)(&.(?:\^.)?)$");
            if (match.Success)
            {
                return FoundThirdPerson(word, ThirdPerson(match.Groups[1].Value) + match.Groups[2].Value, PrependSpace);
            }
            match = Regex.Match(word, @"^(&.(?:\^.)?)(.*?)$");
            if (match.Success)
            {
                return FoundThirdPerson(word, match.Groups[1].Value + ThirdPerson(match.Groups[2].Value), PrependSpace);
            }
            match = Regex.Match(word, "^([*\\-_~'\"/])(.*)(\\1)$");
            if (match.Success)
            {
                return FoundThirdPerson(word, match.Groups[1].Value + ThirdPerson(match.Groups[2].Value) + match.Groups[3].Value, PrependSpace);
            }
            match = Regex.Match(word, @"(.*?)( +)$");
            if (match.Success)
            {
                return FoundThirdPerson(word, ThirdPerson(match.Groups[1].Value) + match.Groups[2].Value, PrependSpace);
            }
            match = Regex.Match(word, @"^( +)(.*?)$");
            if (match.Success)
            {
                return FoundThirdPerson(word, match.Groups[1].Value + ThirdPerson(match.Groups[2].Value), PrependSpace);
            }
            match = Regex.Match(word, @"^(.+)( (?:and|or) )(.+)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return FoundThirdPerson(word, ThirdPerson(match.Groups[1].Value) + match.Groups[2].Value + ThirdPerson(match.Groups[3].Value), PrependSpace);
            }
            if (word.Contains(" "))
            {
                string[] words = word.Split(' ');
                StringBuilder build = Event.NewStringBuilder();
                for (int i = 0; i < words.Length - 1; i++)
                {
                    build.Append(words[i]);
                    build.Append(" ");
                }
                build.Append(ThirdPerson(words[words.Length - 1]));
                return FoundThirdPerson(word, build.ToString(), PrependSpace);
            }
            else
            {
                match = Regex.Match(word, @"^(.*)-(.*)$");
                if (match.Success)
                {
                    return FoundThirdPerson(word, match.Groups[1].Value + "-" + ThirdPerson(match.Groups[2].Value), PrependSpace);
                }
            }

            if (ColorUtility.HasUpperExceptFormatting(word))
            {
                if (ColorUtility.IsAllUpperExceptFormatting(word))
                {
                    return FoundThirdPerson(word, ColorUtility.ToUpperExceptFormatting(ThirdPerson(ColorUtility.ToLowerExceptFormatting(word))), PrependSpace);
                }
                if (ColorUtility.IsFirstUpperExceptFormatting(word))
                {
                    return FoundThirdPerson(word, ColorUtility.CapitalizeExceptFormatting(ThirdPerson(ColorUtility.ToLowerExceptFormatting(word))), PrependSpace);
                }
                return FoundThirdPerson(word, ThirdPerson(ColorUtility.ToLowerExceptFormatting(word)), PrependSpace);
            }

            if (word.Length == 1)
            {
                return FoundThirdPerson(word, word + "s", PrependSpace);
            }

            char last = word[word.Length - 1];
            char secLast = word[word.Length - 2];
            string lastTwo = word.Substring(word.Length - 2, 2);
            thirdPerson = word;
            if (last == 'z' && (secLast == 'a' || secLast == 'e' || secLast == 'i' || secLast == 'o' || secLast == 'u'))
            {
                thirdPerson += "z";
            }
            if (last == 's' || last == 'x' || last == 'z' || lastTwo == "sh" || lastTwo == "ss" || lastTwo == "ch" || (last == 'o' && secLast != 'o' && secLast != 'b'))
            {
                thirdPerson += "es";
            }
            else if (last == 'y' && secLast != 'a' && secLast != 'e' && secLast != 'i' && secLast != 'o' && secLast != 'u')
            {
                thirdPerson = thirdPerson.Substring(0, thirdPerson.Length - 1) + "ies";
            }
            else
            {
                thirdPerson += "s";
            }
            return FoundThirdPerson(word, thirdPerson, PrependSpace);
        }

        /**
         * support method for ThirdPerson()
         */
        private static string FoundThirdPerson(string firstPerson, string thirdPerson, bool PrependSpace)
        {
            if (!firstPersonToThirdPerson.ContainsKey(firstPerson))
            {
                firstPersonToThirdPerson.Add(firstPerson, thirdPerson);
            }
            if (!thirdPersonToFirstPerson.ContainsKey(thirdPerson))
            {
                thirdPersonToFirstPerson.Add(thirdPerson, firstPerson);
            }
            if (PrependSpace)
            {
                thirdPerson = " " + thirdPerson;
                if (!firstPersonToThirdPersonWithSpace.ContainsKey(firstPerson))
                {
                    firstPersonToThirdPersonWithSpace.Add(firstPerson, thirdPerson);
                }
            }
            return thirdPerson;
        }

        public static string PastTenseOf(string verb)
        {
            if (verb == "sleep")
            {
                return "slept";
            }
            if (verb == "sit")
            {
                return "sat";
            }
            if (verb == "drink")
            {
                return "drank";
            }
            if (verb == "put")
            {
                return "put";
            }
            if (verb == "are")
            {
                return "was";
            }
            if (verb == "have")
            {
                return "had";
            }
            if (verb == "eat")
            {
                return "ate";
            }
            if (verb.EndsWith("e"))
            {
                return verb + "d";
            }
            return verb + "ed";
        }

        public static string CardinalNo(int num)
        {
            return (num == 0 ? "no" : Cardinal(num));
        }

        public static string CardinalNo(long num)
        {
            return (num == 0 ? "no" : Cardinal(num));
        }

        public static string Cardinal(int num)
        {
            if (num == 0)
            {
                return "zero";
            }
            StringBuilder result = Event.NewStringBuilder();
            if (num < 0)
            {
                result.Append("negative");
                num = -num;
            }
            int magnitude = (int) Math.Floor(Math.Log10(num));
            ProcessMagnitudes(ref num, ref magnitude, result);
            if (num >= 20)
            {
                int ones = num % 10;
                int tens = (num - ones) / 10;
                num = ones;
                switch (tens)
                {
                case 2: result.Append("twent"); break;
                case 3: result.Append("thirt"); break;
                case 4: result.Append("fort"); break;
                case 5: result.Append("fift"); break;
                case 6: result.Append("sixt"); break;
                case 7: result.Append("sevent"); break;
                case 8: result.Append("eight"); break;
                case 9: result.Append("ninet"); break;
                }
                if (num == 0)
                {
                    result.Append("y");
                    return result.ToString();
                }
                result.Append("y-");
            }
            switch (num)
            {
            case 1: result.Append("one"); break;
            case 2: result.Append("two"); break;
            case 3: result.Append("three"); break;
            case 4: result.Append("four"); break;
            case 5: result.Append("five"); break;
            case 6: result.Append("six"); break;
            case 7: result.Append("seven"); break;
            case 8: result.Append("eight"); break;
            case 9: result.Append("nine"); break;
            case 10: result.Append("ten"); break;
            case 11: result.Append("eleven"); break;
            case 12: result.Append("twelve"); break;
            case 13: result.Append("thirteen"); break;
            case 14: result.Append("fourteen"); break;
            case 15: result.Append("fifteen"); break;
            case 16: result.Append("sixteen"); break;
            case 17: result.Append("seventeen"); break;
            case 18: result.Append("eighteen"); break;
            case 19: result.Append("nineteen"); break;
            }
            return result.ToString();
        }

        public static string Cardinal(long num)
        {
            if (num == 0)
            {
                return "zero";
            }
            StringBuilder result = Event.NewStringBuilder();
            if (num < 0)
            {
                result.Append("negative");
                num = -num;
            }
            int magnitude = (int) Math.Floor(Math.Log10(num));
            ProcessMagnitudes(ref num, ref magnitude, result);
            if (num >= 20)
            {
                long ones = num % 10;
                long tens = (num - ones) / 10;
                num = ones;
                switch (tens)
                {
                case 2: result.Append("twent"); break;
                case 3: result.Append("thirt"); break;
                case 4: result.Append("fort"); break;
                case 5: result.Append("fift"); break;
                case 6: result.Append("sixt"); break;
                case 7: result.Append("sevent"); break;
                case 8: result.Append("eight"); break;
                case 9: result.Append("ninet"); break;
                }
                if (num == 0)
                {
                    result.Append("y");
                    return result.ToString();
                }
                result.Append("y-");
            }
            switch (num)
            {
            case 1: result.Append("one"); break;
            case 2: result.Append("two"); break;
            case 3: result.Append("three"); break;
            case 4: result.Append("four"); break;
            case 5: result.Append("five"); break;
            case 6: result.Append("six"); break;
            case 7: result.Append("seven"); break;
            case 8: result.Append("eight"); break;
            case 9: result.Append("nine"); break;
            case 10: result.Append("ten"); break;
            case 11: result.Append("eleven"); break;
            case 12: result.Append("twelve"); break;
            case 13: result.Append("thirteen"); break;
            case 14: result.Append("fourteen"); break;
            case 15: result.Append("fifteen"); break;
            case 16: result.Append("sixteen"); break;
            case 17: result.Append("seventeen"); break;
            case 18: result.Append("eighteen"); break;
            case 19: result.Append("nineteen"); break;
            }
            return result.ToString();
        }

        public static string Ordinal(int num)
        {
            if (num == 0)
            {
                return "zeroth";
            }
            SB1.Length = 0;
            if (num < 0)
            {
                SB1.Append("negative");
                num = -num;
            }
            int magnitude = (int) Math.Floor(Math.Log10(num));
            ProcessMagnitudes(ref num, ref magnitude, SB1, "th");
            if (num >= 20)
            {
                int ones = num % 10;
                int tens = (num - ones) / 10;
                num = ones;
                switch (tens)
                {
                case 2: SB1.Append("twent"); break;
                case 3: SB1.Append("thirt"); break;
                case 4: SB1.Append("fort"); break;
                case 5: SB1.Append("fift"); break;
                case 6: SB1.Append("sixt"); break;
                case 7: SB1.Append("sevent"); break;
                case 8: SB1.Append("eight"); break;
                case 9: SB1.Append("ninet"); break;
                }
                if (num == 0)
                {
                    SB1.Append("ieth");
                    return SB1.ToString();
                }
                SB1.Append("y-");
            }
            switch (num)
            {
            case 1: SB1.Append("first"); break;
            case 2: SB1.Append("second"); break;
            case 3: SB1.Append("third"); break;
            case 4: SB1.Append("fourth"); break;
            case 5: SB1.Append("fifth"); break;
            case 6: SB1.Append("sixth"); break;
            case 7: SB1.Append("seventh"); break;
            case 8: SB1.Append("eighth"); break;
            case 9: SB1.Append("ninth"); break;
            case 10: SB1.Append("tenth"); break;
            case 11: SB1.Append("eleventh"); break;
            case 12: SB1.Append("twelfth"); break;
            case 13: SB1.Append("thirteenth"); break;
            case 14: SB1.Append("fourteenth"); break;
            case 15: SB1.Append("fifteenth"); break;
            case 16: SB1.Append("sixteenth"); break;
            case 17: SB1.Append("seventeenth"); break;
            case 18: SB1.Append("eighteenth"); break;
            case 19: SB1.Append("nineteenth"); break;
            }
            return SB1.ToString();
        }

        public static string Ordinal(long num)
        {
            if (num == 0)
            {
                return "zeroth";
            }
            SB1.Length = 0;
            if (num < 0)
            {
                SB1.Append("negative");
                num = -num;
            }
            int magnitude = (int) Math.Floor(Math.Log10(num));
            ProcessMagnitudes(ref num, ref magnitude, SB1, "th");
            if (num >= 20)
            {
                long ones = num % 10;
                long tens = (num - ones) / 10;
                num = ones;
                switch (tens)
                {
                case 2: SB1.Append("twent"); break;
                case 3: SB1.Append("thirt"); break;
                case 4: SB1.Append("fort"); break;
                case 5: SB1.Append("fift"); break;
                case 6: SB1.Append("sixt"); break;
                case 7: SB1.Append("sevent"); break;
                case 8: SB1.Append("eight"); break;
                case 9: SB1.Append("ninet"); break;
                }
                if (num == 0)
                {
                    SB1.Append("ieth");
                    return SB1.ToString();
                }
                SB1.Append("y-");
            }
            switch (num)
            {
            case 1: SB1.Append("first"); break;
            case 2: SB1.Append("second"); break;
            case 3: SB1.Append("third"); break;
            case 4: SB1.Append("fourth"); break;
            case 5: SB1.Append("fifth"); break;
            case 6: SB1.Append("sixth"); break;
            case 7: SB1.Append("seventh"); break;
            case 8: SB1.Append("eighth"); break;
            case 9: SB1.Append("ninth"); break;
            case 10: SB1.Append("tenth"); break;
            case 11: SB1.Append("eleventh"); break;
            case 12: SB1.Append("twelfth"); break;
            case 13: SB1.Append("thirteenth"); break;
            case 14: SB1.Append("fourteenth"); break;
            case 15: SB1.Append("fifteenth"); break;
            case 16: SB1.Append("sixteenth"); break;
            case 17: SB1.Append("seventeenth"); break;
            case 18: SB1.Append("eighteenth"); break;
            case 19: SB1.Append("nineteenth"); break;
            }
            return SB1.ToString();
        }

        /**
         * support method for Cardinal() and Ordinal()
         */
        private static void ProcessMagnitude(ref int num, ref int magnitude, StringBuilder result, string place)
        {
            if (magnitude > 3)
            {
                magnitude -= magnitude % 3;
            }
            int offset = (int) Math.Floor(Math.Exp(magnitude * Math.Log(10)));
            int remainder = num % offset;
            int val = num - remainder;
            int count = val / offset;
            if (count > 0)
            {
                result.Append(Cardinal(count));
                result.Append(" ");
                result.Append(place);
                num = remainder;
                if (num > 0)
                {
                    result.Append(" ");
                }
            }
            magnitude--;
        }

        /**
         * support method for Cardinal() and Ordinal()
         */
        private static void ProcessMagnitude(ref long num, ref int magnitude, StringBuilder result, string place)
        {
            if (magnitude > 3)
            {
                magnitude -= magnitude % 3;
            }
            int offset = (int) Math.Floor(Math.Exp(magnitude * Math.Log(10)));
            long remainder = num % offset;
            long val = num - remainder;
            long count = val / offset;
            if (count > 0)
            {
                result.Append(Cardinal(count));
                result.Append(" ");
                result.Append(place);
                num = remainder;
                if (num > 0)
                {
                    result.Append(" ");
                }
            }
            magnitude--;
        }

        /**
         * support method for Cardinal() and Ordinal()
         */
        private static bool ProcessMagnitudes(ref int num, ref int magnitude, StringBuilder result, string suffix = null)
        {
            switch (magnitude)
            {
            case 20:
            case 19:
            case 18:
                ProcessMagnitude(ref num, ref magnitude, result, "quintillion");
                if (num == 0)
                {
                    if (suffix != null)
                    {
                        result.Append(suffix);
                    }
                    return true;
                }
                goto case 15;
            case 17:
            case 16:
            case 15:
                ProcessMagnitude(ref num, ref magnitude, result, "quadrillion");
                if (num == 0)
                {
                    if (suffix != null)
                    {
                        result.Append(suffix);
                    }
                    return true;
                }
                goto case 12;
            case 14:
            case 13:
            case 12:
                ProcessMagnitude(ref num, ref magnitude, result, "trillion");
                if (num == 0)
                {
                    if (suffix != null)
                    {
                        result.Append(suffix);
                    }
                    return true;
                }
                goto case 9;
            case 11:
            case 10:
            case 9:
                ProcessMagnitude(ref num, ref magnitude, result, "billion");
                if (num == 0)
                {
                    if (suffix != null)
                    {
                        result.Append(suffix);
                    }
                    return true;
                }
                goto case 6;
            case 8:
            case 7:
            case 6:
                ProcessMagnitude(ref num, ref magnitude, result, "million");
                if (num == 0)
                {
                    if (suffix != null)
                    {
                        result.Append(suffix);
                    }
                    return true;
                }
                goto case 3;
            case 5:
            case 4:
            case 3:
                ProcessMagnitude(ref num, ref magnitude, result, "thousand");
                if (num == 0)
                {
                    if (suffix != null)
                    {
                        result.Append(suffix);
                    }
                    return true;
                }
                goto case 2;
            case 2:
                if (magnitude > 1)
                {
                    ProcessMagnitude(ref num, ref magnitude, result, "hundred");
                    if (num == 0)
                    {
                        if (suffix != null)
                        {
                            result.Append(suffix);
                        }
                        return true;
                    }
                }
                break;
            }
            return false;
        }

        /**
         * support method for Cardinal() and Ordinal()
         */
        private static bool ProcessMagnitudes(ref long num, ref int magnitude, StringBuilder result, string suffix = null)
        {
            switch (magnitude)
            {
            case 20:
            case 19:
            case 18:
                ProcessMagnitude(ref num, ref magnitude, result, "quintillion");
                if (num == 0)
                {
                    if (suffix != null)
                    {
                        result.Append(suffix);
                    }
                    return true;
                }
                goto case 15;
            case 17:
            case 16:
            case 15:
                ProcessMagnitude(ref num, ref magnitude, result, "quadrillion");
                if (num == 0)
                {
                    if (suffix != null)
                    {
                        result.Append(suffix);
                    }
                    return true;
                }
                goto case 12;
            case 14:
            case 13:
            case 12:
                ProcessMagnitude(ref num, ref magnitude, result, "trillion");
                if (num == 0)
                {
                    if (suffix != null)
                    {
                        result.Append(suffix);
                    }
                    return true;
                }
                goto case 9;
            case 11:
            case 10:
            case 9:
                ProcessMagnitude(ref num, ref magnitude, result, "billion");
                if (num == 0)
                {
                    if (suffix != null)
                    {
                        result.Append(suffix);
                    }
                    return true;
                }
                goto case 6;
            case 8:
            case 7:
            case 6:
                ProcessMagnitude(ref num, ref magnitude, result, "million");
                if (num == 0)
                {
                    if (suffix != null)
                    {
                        result.Append(suffix);
                    }
                    return true;
                }
                goto case 3;
            case 5:
            case 4:
            case 3:
                ProcessMagnitude(ref num, ref magnitude, result, "thousand");
                if (num == 0)
                {
                    if (suffix != null)
                    {
                        result.Append(suffix);
                    }
                    return true;
                }
                goto case 2;
            case 2:
                if (magnitude > 1)
                {
                    ProcessMagnitude(ref num, ref magnitude, result, "hundred");
                    if (num == 0)
                    {
                        if (suffix != null)
                        {
                            result.Append(suffix);
                        }
                        return true;
                    }
                }
                break;
            }
            return false;
        }

        public static string Multiplicative(int num)
        {
            switch (num)
            {
                case 0:
                    return "never";
                case 1:
                    return "once";
                case 2:
                    return "twice";
                case 3:
                    return "thrice";
            }
            return Cardinal(num) + " times";
        }

        public static string Multiplicative(long num)
        {
            switch (num)
            {
                case 0:
                    return "never";
                case 1:
                    return "once";
                case 2:
                    return "twice";
                case 3:
                    return "thrice";
            }
            return Cardinal(num) + " times";
        }

        public static string MakeOrList(string[] Words, bool Serial = true)
        {
            if (Words.Length == 0)
            {
                return "";
            }
            if (Words.Length == 1)
            {
                return Words[0];
            }
            if (Words.Length == 2)
            {
                return Words[0] + " or " + Words[1];
            }
            string separator = ",";
            for (int i = 0, j = Words.Length; i < j; i++)
            {
                if (Words[i].Contains(','))
                {
                    separator = ";";
                    break;
                }
            }
            SB1.Length = 0;
            for (int i = 0, j = Words.Length - 2; i < j; i++)
            {
                SB1.Append(Words[i]);
                SB1.Append((Serial || i < j - 1) ? separator : ' ');
            }
            SB1.Append(Words[Words.Length - 1]);
            return SB1.ToString();
        }

        public static string MakeOrList(List<string> Words, bool Serial = true)
        {
            if (Words.Count == 0)
            {
                return "";
            }
            if (Words.Count == 1)
            {
                return Words[0];
            }
            if (Words.Count == 2)
            {
                return Words[0] + " or " + Words[1];
            }
            string separator = ", ";
            for (int i = 0, j = Words.Count; i < j; i++)
            {
                if (Words[i].Contains(','))
                {
                    separator = "; ";
                    break;
                }
            }
            SB1.Length = 0;
            for (int i = 0, j = Words.Count - 1; i < j; i++ )
            {
                SB1.Append(Words[i]);
                SB1.Append((Serial || i < j - 1) ? separator : ' ');
            }
            SB1.Append("or ");
            SB1.Append(Words[Words.Count - 1]);
            return SB1.ToString();
        }

        public static string MakeOrList(
            List<GameObject> Objects,
            bool DefiniteArticles = false,
            bool Serial = true,
            bool Reflexive = false,
            bool AsPossessed = false,
            bool SecondPerson = true,
            GameObject AsPossessedBy = null
        )
        {
            if (Objects == null || Objects.Count == 0)
            {
                return "";
            }
            List<string> names = new List<string>(Objects.Count);
            foreach (GameObject obj in Objects)
            {
                names.Add(
                    obj.GetDisplayName(
                        Short: true,
                        WithDefiniteArticle: DefiniteArticles && !AsPossessed,
                        WithIndefiniteArticle: !DefiniteArticles && !AsPossessed,
                        Reflexive: Reflexive,
                        SecondPerson: SecondPerson,
                        AsPossessed: AsPossessed,
                        AsPossessedBy: AsPossessedBy
                    )
                );
            }
            return MakeOrList(names, Serial);
        }

        public static string MakeAndList(IReadOnlyList<string> Words, bool Serial = true)
        {
            if (Words.Count == 0)
            {
                return "";
            }
            if (Words.Count == 1)
            {
                return Words[0];
            }
            if (Words.Count == 2)
            {
                return Words[0] + " and " + Words[1];
            }
            string separator = ", ";
            for (int i = 0, j = Words.Count; i < j; i++)
            {
                if (Words[i].Contains(','))
                {
                    separator = "; ";
                    break;
                }
            }
            SB1.Length = 0;
            for (int i = 0, j = Words.Count - 1; i < j; i++)
            {
                SB1.Append(Words[i]);
                SB1.Append((Serial || i < j - 1) ? separator : ' ');
            }
            SB1.Append("and ");
            SB1.Append(Words[Words.Count - 1]);
            return SB1.ToString();
        }

        public static string MakeAndList(
            List<GameObject> Objects,
            bool DefiniteArticles = false,
            bool Serial = true,
            bool Reflexive = false,
            bool SecondPerson = true,
            bool AsPossessed = false,
            GameObject AsPossessedBy = null
        )
        {
            if (Objects == null || Objects.Count == 0)
            {
                return "";
            }
            List<string> names = new List<string>(Objects.Count);
            foreach (GameObject obj in Objects)
            {
                names.Add(
                    obj.GetDisplayName(
                        Short: true,
                        WithDefiniteArticle: DefiniteArticles && !AsPossessed,
                        WithIndefiniteArticle: !DefiniteArticles && !AsPossessed,
                        Reflexive: Reflexive,
                        SecondPerson: SecondPerson,
                        AsPossessed: AsPossessed,
                        AsPossessedBy: AsPossessedBy
                    )
                );
            }
            return MakeAndList(names, Serial);
        }

        public static string MakeTheList(IReadOnlyList<string> Words, bool Capitalize = false)
        {
            if (Words.Count == 0)
            {
                return "";
            }

            var separator = ", ";
            for (int i = 0, c = Words.Count; i < c; i++)
            {
                if (Words[i].Contains(','))
                {
                    separator = "; ";
                    break;
                }
            }

            var sb = SB1;
            sb.Length = 0;
            for (int i = 0, c = Words.Count; i < c; i++)
            {
                if (i != 0) sb.Append(separator);
                sb.Append(Capitalize && i == 0 ? "The " : "the ");
                sb.Append(Words[i]);
            }

            return sb.ToString();
        }

        public static string MakePossessive(string word)
        {
            int wrap = 0;
            while (word.EndsWith("}}"))
            {
                wrap++;
                word = word.Substring(0, word.Length - 2);
            }
            if (word == "you")
            {
                word = "your";
            }
            else
            if (word == "You")
            {
                word = "Your";
            }
            else
            if (word == "YOU")
            {
                word = "YOUR";
            }
            else
            if (word.EndsWith("s"))
            {
                word += "'";
            }
            else
            {
                word += "'s";
            }
            for (int i = 0; i < wrap; i++)
            {
                word += "}}";
            }
            return word;
        }

        public static string MakeCompoundWord(string Word1, string Word2, bool UseHyphen = false)
        {
            SB1.Clear();

            if (Char.Equals(Word1[Word1.Count() - 1], Word2[0]) || UseHyphen)
            {
                SB1.Append(Word1);
                SB1.Append("-");
                SB1.Append(Word2);
            }
            else
            {
                SB1.Append(Word1);
                SB1.Append(Word2);
            }

            return SB1.ToString();
        }

        public static string MakeTitleCase(string Phrase)
        {
            string[] words = Phrase.Split(' ');
            bool isFirstWord = true;
            SB1.Clear();
            foreach (string word in words)
            {
                if (isFirstWord)
                {
                    SB1.Append(InitialCap(word));
                    isFirstWord = false;
                }
                else
                if (IsLowerCapWord(word))
                {
                    SB1.Append(word);
                }
                else
                {
                    SB1.Append(InitialCap(word));
                }
                SB1.Append(" ");
            }
            return SB1.ToString().TrimEnd(' ');
        }

        public static string MakeLowerCase(string Phrase)
        {
            string[] words = Phrase.Split(' ');
            SB1.Clear();
            foreach (string word in words)
            {
                SB1
                    .Append(InitLower(word))
                    .Append(" ")
                ;
            }
            return SB1.ToString().TrimEnd(' ');
        }

        public static string InitialCap(string Word)
        {
            if (Word.IsNullOrEmpty())
            {
                return Word;
            }
            if (Word.Contains("-"))
            {
                string[] pieces = Word.Split('-');
                for (int i = 0; i < pieces.Length; i++)
                {
                    if (pieces[i].Length > 1)
                    {
                        pieces[i] = pieces[i].Capitalize();
                    }
                    else
                    if (i == 0)
                    {
                        pieces[i] = pieces[i].ToUpper();
                    }
                }
                return string.Join("-", pieces);
            }
            return Word.Capitalize();
        }

        public static bool IsSupportingWord(string Word)
        {
            string LowerWord = Word.Any(char.IsUpper) ? Word.ToLower() : Word;
            return
                prepositions.Contains(LowerWord)
                || articles.Contains(LowerWord)
                || conjunctions.Contains(LowerWord)
                || badEndingWords.Contains(LowerWord)
                || badStartingWords.Contains(LowerWord)
            ;
        }

        public static bool IsLowerCapWord(string Word)
        {
            string LowerWord = Word.Any(char.IsUpper) ? Word.ToLower() : Word;
            return
                shortPrepositions.Contains(LowerWord)
                || articles.Contains(LowerWord)
                || conjunctions.Contains(LowerWord)
            ;
        }

        public static bool IsBadTitleStartingWord(string Word)
        {
            string LowerWord = Word.Any(char.IsUpper) ? Word.ToLower() : Word;
            return badStartingWords.Contains(LowerWord);
        }

        private static bool IsArticleException(string Word)
        {
            if( Word == null || articleExceptions == null ) return false;
            
            for( int x=0;x<articleExceptions.Length;x++ )
            {
                if( String.Equals( articleExceptions[x], Word, StringComparison.OrdinalIgnoreCase ) ) return true;
            }
            return false;
        }

        public static string RemoveBadTitleEndingWords(string Phrase)
        {
            string[] words = Phrase.Split(' ');
            if (IsSupportingWord(words[words.Length - 1]))
            {
                words[words.Length - 1] = "";
                return RemoveBadTitleEndingWords(string.Join(" ", words).TrimEnd(' '));
            }
            else return Phrase;
        }

        public static string RemoveBadTitleStartingWords(string Phrase)
        {
            string[] words = Phrase.Split(' ');
            if (IsBadTitleStartingWord(words[0]))
            {
                words[0] = "";
                return RemoveBadTitleStartingWords(string.Join(" ", words).TrimStart(' '));
            }
            else return Phrase;
        }

        public static string RandomShePronoun()
        {
            return 50.in100() ? "he" : "she";
        }

        public static string ObjectPronoun(string subjectPronoun)
        {
            if (subjectPronoun == "he")
            {
                return "him";
            }
            else
            {
                return "her";
            }
        }

        public static string PossessivePronoun(string subjectPronoun)
        {
            if (subjectPronoun == "he")
            {
                return "his";
            }
            else
            {
                return "her";
            }
        }

        public static string ReflexivePronoun(string subjectPronoun)
        {
            if (subjectPronoun == "he")
            {
                return "himself";
            }
            else
            {
                return "herself";
            }
        }

        public static string InitCap(string word)
        {
            if (word.IsNullOrEmpty())
            {
                return "";
            }
            if (Char.IsUpper(word[0]))
            {
                return word;
            }

            var len = word.Length;
            if (len == 1)
            {
                return Char.ToUpper(word[0]).ToString();
            }

            if (len < 64)
            {
                Span<char> span = stackalloc char[len];
                word.AsSpan().CopyTo(span);
                span[0] = char.ToUpper(span[0]);
                return new string(span);
            }

            char[] a = word.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        public static string InitCapWithFormatting(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return "";
            }
            if (word.Length == 1)
            {
                return Char.ToUpper(word[0]).ToString();
            }

            char[] a = word.ToCharArray();

            for (int x = 0; x < word.Length; x++)
            {
                if (word[x] == '&' || word[x] == '^')
                {
                    x++;
                }
                else
                {
                    a[x] = char.ToUpper( a[x] );
                    break;
                }
            }
            return new string( a );
        }

        public static string CapAfterNewlines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }
            SB1.Length = 0;
            string[] sentences = text.Split('\n');
            for (int i = 0, j = sentences.Length; i < j; i++)
            {
                SB1.Append(InitCap(sentences[i]));
                if (i < j - 1)
                {
                    SB1.Append('\n');
                }
            }
            return SB1.ToString();
        }

        public static string InitLower(string Word)
        {
            if (Word.IsNullOrEmpty())
            {
                return "";
            }
            if (Char.IsLower(Word[0]))
            {
                return Word;
            }
            if (Word.Length == 1)
            {
                return Char.ToLower(Word[0]).ToString();
            }
            return ColorUtility.UncapitalizeExceptFormatting(Word);
        }

        public static string InitLowerIfArticle(string Word)
        {
            if (
                ColorUtility.BeginsWithExceptFormatting(Word, "A ")
                || ColorUtility.BeginsWithExceptFormatting(Word, "An ")
                || ColorUtility.BeginsWithExceptFormatting(Word, "The ")
                || ColorUtility.BeginsWithExceptFormatting(Word, "Some ")
            )
            {
                return ColorUtility.UncapitalizeExceptFormatting(Word);
            }
            return Word;
        }

        public static string LowerArticles(string Text)
        {
            Text = Regex.Replace(Text, @"\bA\b", "a");
            Text = Regex.Replace(Text, @"\bAn\b", "an");
            Text = Regex.Replace(Text, @"\bThe\b", "the");
            Text = Regex.Replace(Text, @"\bSome\b", "some");
            return Text;
        }

        public static string MakeTitleCaseWithArticle(string phrase)
        {
            if (
                phrase.StartsWith("a ")
                || phrase.StartsWith("A ")
                || phrase.StartsWith("an ")
                || phrase.StartsWith("An ")
                || phrase.StartsWith("the ")
                || phrase.StartsWith("The ")
                || phrase.StartsWith("some ")
                || phrase.StartsWith("Some ")
            )
            {
                string capPhrase = MakeTitleCase(phrase);
                return Char.ToLower(capPhrase[0]).ToString() + capPhrase.Substring(1);
            }
            else
            {
                return MakeTitleCase(phrase);
            }
        }

        private static bool IsNonWord(char Ch)
        {
            if (char.IsLetterOrDigit(Ch))
            {
                return false;
            }
            if (Ch == '-')
            {
                return false;
            }
            return true;
        }

        public static bool IndefiniteArticleShouldBeAn(string Word)
        {
            if (Word.IsNullOrEmpty())
            {
                return false;
            }
            string useWord = Word;
            if (ColorUtility.HasFormatting(useWord))
            {
                useWord = ColorUtility.StripFormatting(useWord);
            }

            int spaceIndex = useWord.IndexOf(' ');
            if (spaceIndex == 0) useWord = useWord.TrimStart(' ');
            spaceIndex = useWord.IndexOf(' ');
            if (spaceIndex != -1)
            {
                useWord = useWord.Substring(0, spaceIndex);
            }

            bool isNonWord = false;
            for( int x=0;x<useWord.Length;x++ )
            {
                if( IsNonWord( useWord[x] ) )
                {
                    isNonWord = true;
                    break;
                }
            }

            if (isNonWord)
            {
                SB2.Clear();
                for (int i = 0, j = useWord.Length; i < j; i++)
                {
                    if (!IsNonWord(useWord[i]))
                    {
                        SB2.Append(useWord[i]);
                    }
                }
                useWord = SB2.ToString();
            }
            if (useWord.Length > 0 && useWord[0] == '-') useWord = useWord.TrimStart('-');

            if (useWord == "")
            {
                return false;
            }

            char first = useWord[0];
            return
                ((first == 'a' || first == 'e' || first == 'i' || first == 'o' || first == 'u' || first == 'A' || first == 'E' || first == 'I' || first == 'O' || first == 'U') ^ IsArticleException(useWord))
                && !useWord.StartsWith("one-", StringComparison.OrdinalIgnoreCase)
            ;
        }

        public static bool IndefiniteArticleShouldBeAn(int Number)
        {
            return IndefiniteArticleShouldBeAn(Cardinal(Number));
        }

        public static string IndefiniteArticle(string Word, bool Capitalize = false)
        {
            return IndefiniteArticleShouldBeAn(Word) ? (Capitalize ? "An " : "an ") : (Capitalize ? "A " : "a ");
        }

        public static string A(string Word, bool Capitalize = false)
        {
            if (Word[0] == '=')
            {
                return "=article=" + Word;
            }
            return (IndefiniteArticleShouldBeAn(Word) ? (Capitalize ? "An " : "an ") : (Capitalize ? "A " : "a ")) + Word;
        }

        public static void A(string Word, StringBuilder Result, bool Capitalize = false)
        {
            if (Word[0] == '=')
            {
                Result.Append("=article=").Append(Word);
            }
            else
            {
                Result
                    .Append(IndefiniteArticleShouldBeAn(Word) ? (Capitalize ? "An " : "an ") : (Capitalize ? "A " : "a "))
                    .Append(Word)
                ;
            }
        }

        public static string A(int Number, bool Capitalize = false)
        {
            return (IndefiniteArticleShouldBeAn(Number) ? (Capitalize ? "An " : "an ") : (Capitalize ? "A " : "a ")) + Number;
        }

        public static void A(int Number, StringBuilder Result, bool Capitalize = false)
        {
            Result
                .Append(IndefiniteArticleShouldBeAn(Number) ? (Capitalize ? "An " : "an ") : (Capitalize ? "A " : "a "))
                .Append(Number)
            ;
        }

        public static string ConvertAtoAn(string sentence)
        {
            string[] words = sentence.Split(' ');
            SB1.Clear();

            for (int i = 0; i < words.Length; i++)
            {
                SB1.Append(words[i]);
                if (i < words.Length - 1)
                {
                    if ((words[i].Equals("a") || words[i].Equals("A")) && !string.IsNullOrEmpty(words[i + 1]) && IndefiniteArticleShouldBeAn(words[i + 1]))
                    {
                        SB1.Append("n");
                    }
                    SB1.Append(" ");
                }
            }

            return SB1.ToString();
        }

        public static string AOrAnBeforeNumber(int number)
        {
            if (number == 11)
            {
                return "an";
            }
            while (number >= 10)
            {
                number /= 10;
            }
            if (number == 8)
            {
                return "an";
            }
            else
            {
                return "a";
            }
        }

        public static string GetWordRoot(string word)
        {
            string objWord = GetRandomMeaningfulWord(word);

            string result = "";
            String[] substrings = Regex.Split(objWord, @"(?=[aeiouy])");

            for (int i = 0; i <= substrings.Length - 1; i++)
            {
                if (i == substrings.Length - 1)
                {
                    result += substrings[i].TrimEnd(new char[] { 'a', 'e', 'i', 'o', 'u', 'y' });
                }
                else result += substrings[i];
            }

            return result;
        }

        public static string Adjectify(string word)
        {
            string suffix = new string[] { "ian", "ic", "-like", "ary", "ique" }.GetRandomElement();
            word = TrimTrailingS(word);
            word = RemovePunctuation(word);
            if (suffix[0] != '-')
            {
                word = GetWordRoot(word);
            }
            return word + suffix;
        }

        public static string TrimLeadingThe(string phrase)
        {
            if (phrase.StartsWith("the ") && phrase.Length >= 5)
            {
                return phrase.Substring(4);
            }
            else return phrase;
        }

        public static string TrimTrailingS(string word)
        {
            if (word[word.Length - 1] == 's')
            {
                return word.Substring(0, word.Length - 1);
            }
            return word;
        }

        public static string TrimTrailingPunctuation( string phrase )
        {
            if (phrase.Length == 0)
            {
                return phrase;
            }
            if (punctuation.Contains(phrase[phrase.Length - 1]))
            {
                return TrimTrailingPunctuation(phrase.Substring(0, phrase.Length - 1));
            }
            else
            {
                return phrase;
            }
        }

        public static string RemovePunctuation(string word)
        {
            foreach (char c in punctuation)
            {
                if (word.IndexOf(c) != -1)
                {
                    word = word.Replace("" + c, "");
                }
            }
            return word;
        }

        public static string GetRandomMeaningfulWord(string phrase)
        {
            String[] splitWords = phrase.Split(' ');
            string word = splitWords[Rules.Stat.Random(0, splitWords.Length - 1)];
            int tries = 500;

            while (prepositions.Contains(word) || articles.Contains(word) || conjunctions.Contains(word) || ordinalsRoman.Contains(word) || demonstrativePronouns.Contains(word) && tries > 0)
            {
                word = splitWords[Rules.Stat.Random(0, splitWords.Length - 1)];
                tries--;
            }
            return word;
        }

        public static string GetWeightedStartingArticle()
        {
            return articleStartingWords.GetRandomElement();
        }

        private static StringBuilder StutterSB = new();

        public static string Stutterize(string Sentence, string Word)
        {
            int spaceCount = 0;
            bool inCode = false;
            bool inEmote = false;
            char last = '\0';
            for (int i = 0, j = Sentence.Length; i < j; i++)
            {
                char ch = Sentence[i];
                if (ch == '=')
                {
                    inCode = !inCode;
                }
                else
                if (ch == '*')
                {
                    inEmote = !inEmote;
                }
                else
                if (ch == ' ' && !inCode && !inEmote && last != ' ')
                {
                    spaceCount++;
                }
                if (!inCode && !inEmote)
                {
                    last = ch;
                }
            }
            if (spaceCount <= 2)
            {
                return Sentence;
            }
            StutterSB.Clear();
            int stutterAt = Rules.Stat.Random(0, spaceCount - 3);
            int stuttersLeft = (stutterAt == 0) ? 4 : 0;
            int spaceNum = 0;
            inCode = false;
            inEmote = false;
            last = '\0';
            for (int i = 0, j = Sentence.Length; i < j; i++)
            {
                char ch = Sentence[i];
                if (ch == '=')
                {
                    inCode = !inCode;
                }
                else
                if (ch == '*')
                {
                    inEmote = !inEmote;
                }
                else
                if (ch == ' ' && !inCode && !inEmote && last != ' ')
                {
                    spaceNum++;
                    if (spaceNum == stutterAt)
                    {
                        stuttersLeft = 4;
                    }
                    if (stuttersLeft == 1)
                    {
                        StutterSB
                            .Append("... ")
                            .Append(Word)
                        ;
                        stuttersLeft--;
                    }
                    else
                    if (stuttersLeft > 0)
                    {
                        StutterSB.Append("...");
                        stuttersLeft--;
                    }
                }
                StutterSB.Append(ch);
                if (!inCode && !inEmote)
                {
                    last = ch;
                }
            }
            if (stuttersLeft == 1)
            {
                StutterSB
                    .Append("... ")
                    .Append(Word)
                ;
            }
            else
            if (stuttersLeft > 0)
            {
                StutterSB.Append("...");
            }
            return StutterSB.ToString();
        }

        public static string GetProsaicZoneName(Zone Z)
        {
            if (Z.HasProperName)
            {
                return Z.DisplayName;
            }
            else
            if (Z.NameContext != null)
            {
                return "the outskirts of " + Z.NameContext;
            }
            else
            {
                return Z?.GetTerrainObject()?.an(Stripped: true) ?? "an unnamed place";
            }
        }

        public static string GetRomanNumeral(int val)
        {
            if (val == 0)
            {
                return "N";
            }
            SB1.Clear();
            if (val < 0)
            {
                SB1.Append("-");
                val = -val;
            }
            if (val >= 1000)
            {
                for (int i = 0, j = val / 1000; i < j; i++)
                {
                    SB1.Append('M');
                }
                val %= 1000;
            }
            if (val >= 100)
            {
                int part = val / 100;
                switch(part) {
                    case 9:
                        SB1.Append("CM");
                        break;
                    case 6:
                    case 7:
                    case 8:
                        SB1.Append('D');
                        for (int i = 0, j = part - 5; i < j; i++)
                        {
                            SB1.Append('C');
                        }
                        break;
                    case 5:
                        SB1.Append('D');
                        break;
                    case 4:
                        SB1.Append("CD");
                        break;
                    default:
                        for (int i = 0; i < part; i++)
                        {
                            SB1.Append('C');
                        }
                        break;
                }
                val %= 100;
            }
            if (val >= 10)
            {
                int part = val / 10;
                switch(part) {
                    case 9:
                        SB1.Append("XC");
                        break;
                    case 6:
                    case 7:
                    case 8:
                        SB1.Append('L');
                        for (int i = 0, j = part - 5; i < j; i++)
                        {
                            SB1.Append('X');
                        }
                        break;
                    case 5:
                        SB1.Append('L');
                        break;
                    case 4:
                        SB1.Append("XL");
                        break;
                    default:
                        for (int i = 0; i < part; i++)
                        {
                            SB1.Append('X');
                        }
                        break;
                }
                val %= 10;
            }
            switch(val)
            {
                case 9:
                    SB1.Append("IX");
                    break;
                case 6:
                case 7:
                case 8:
                    SB1.Append('V');
                    for (int i = 0, j = val - 5; i < j; i++)
                    {
                        SB1.Append('I');
                    }
                    break;
                case 5:
                    SB1.Append('V');
                    break;
                case 4:
                    SB1.Append("IV");
                    break;
                default:
                    for (int i = 0; i < val; i++)
                    {
                        SB1.Append('I');
                    }
                    break;
            }
            return SB1.ToString();
        }

        public static string GetRomanNumeral(long val)
        {
            if (val == 0)
            {
                return "N";
            }
            SB1.Clear();
            if (val < 0)
            {
                SB1.Append("-");
                val = -val;
            }
            if (val >= 1000)
            {
                for (long i = 0, j = val / 1000; i < j; i++)
                {
                    SB1.Append('M');
                }
                val %= 1000;
            }
            if (val >= 100)
            {
                long part = val / 100;
                switch(part) {
                    case 9:
                        SB1.Append("CM");
                        break;
                    case 6:
                    case 7:
                    case 8:
                        SB1.Append('D');
                        for (long i = 0, j = part - 5; i < j; i++)
                        {
                            SB1.Append('C');
                        }
                        break;
                    case 5:
                        SB1.Append('D');
                        break;
                    case 4:
                        SB1.Append("CD");
                        break;
                    default:
                        for (int i = 0; i < part; i++)
                        {
                            SB1.Append('C');
                        }
                        break;
                }
                val %= 100;
            }
            if (val >= 10)
            {
                long part = val / 10;
                switch(part) {
                    case 9:
                        SB1.Append("XC");
                        break;
                    case 6:
                    case 7:
                    case 8:
                        SB1.Append('L');
                        for (long i = 0, j = part - 5; i < j; i++)
                        {
                            SB1.Append('X');
                        }
                        break;
                    case 5:
                        SB1.Append('L');
                        break;
                    case 4:
                        SB1.Append("XL");
                        break;
                    default:
                        for (long i = 0; i < part; i++)
                        {
                            SB1.Append('X');
                        }
                        break;
                }
                val %= 10;
            }
            switch(val)
            {
                case 9:
                    SB1.Append("IX");
                    break;
                case 6:
                case 7:
                case 8:
                    SB1.Append('V');
                    for (long i = 0, j = val - 5; i < j; i++)
                    {
                        SB1.Append('I');
                    }
                    break;
                case 5:
                    SB1.Append('V');
                    break;
                case 4:
                    SB1.Append("IV");
                    break;
                default:
                    for (long i = 0; i < val; i++)
                    {
                        SB1.Append('I');
                    }
                    break;
            }
            return SB1.ToString();
        }

        public static string Weirdify( string word, int Chance = 100 )
        {
            char a = weirdLowerAs.GetRandomElement();
            char A = weirdUpperAs.GetRandomElement();
            char e = weirdLowerEs.GetRandomElement();
            char E = weirdLowerEs.GetRandomElement();
            char i = weirdLowerIs.GetRandomElement();
            char I = weirdUpperIs.GetRandomElement();
            char o = weirdLowerOs.GetRandomElement();
            char O = weirdUpperOs.GetRandomElement();
            char u = weirdLowerUs.GetRandomElement();
            char U = weirdUpperUs.GetRandomElement();
            char c = weirdLowerCs.GetRandomElement();
            char f = weirdLowerFs.GetRandomElement();
            char n = weirdLowerNs.GetRandomElement();
            char t = weirdLowerTs.GetRandomElement();
            char y = weirdLowerYs.GetRandomElement();
            char B = weirdUpperBs.GetRandomElement();
            char C = weirdUpperCs.GetRandomElement();
            char Y = weirdUpperYs.GetRandomElement();
            char L = weirdUpperLs.GetRandomElement();
            char R = weirdUpperRs.GetRandomElement();
            char N = weirdUpperNs.GetRandomElement();

            if (If.Chance(Chance)) word = word.Replace('a', a);
            if (If.Chance(Chance)) word = word.Replace('A', A);
            if (If.Chance(Chance)) word = word.Replace('e', e);
            if (If.Chance(Chance)) word = word.Replace('E', E);
            if (If.Chance(Chance)) word = word.Replace('i', i);
            if (If.Chance(Chance)) word = word.Replace('I', I);
            if (If.Chance(Chance)) word = word.Replace('o', o);
            if (If.Chance(Chance)) word = word.Replace('O', O);
            if (If.Chance(Chance)) word = word.Replace('u', u);
            if (If.Chance(Chance)) word = word.Replace('U', U);
            if (If.Chance(Chance)) word = word.Replace('c', c);
            if (If.Chance(Chance)) word = word.Replace('f', f);
            if (If.Chance(Chance)) word = word.Replace('n', n);
            if (If.Chance(Chance)) word = word.Replace('t', t);
            if (If.Chance(Chance)) word = word.Replace('y', y);
            if (If.Chance(Chance)) word = word.Replace('B', B);
            if (If.Chance(Chance)) word = word.Replace('C', C);
            if (If.Chance(Chance)) word = word.Replace('Y', Y);
            if (If.Chance(Chance)) word = word.Replace('L', L);
            if (If.Chance(Chance)) word = word.Replace('R', R);
            if (If.Chance(Chance)) word = word.Replace('N', N);

            return word;
        }

        public static string Obfuscate(string phrase, int noiseValue = 15, int sameObfuscatorChance = 80, int onlyObfuscastorsChance = 5)
        {
            StringBuilder SB = new StringBuilder();
            char obfuscatorPrime = obfuscators.GetRandomElement();
            bool bOnlyObfuscators = If.Chance(onlyObfuscastorsChance) ? true : false;

            foreach (char c in phrase)
            {
                if (c == ' ')
                {
                    SB.Append(c);
                    continue;
                }
                if (If.Chance(noiseValue))
                {
                    if (If.Chance(sameObfuscatorChance))
                    {
                        SB.Append(obfuscatorPrime);
                    }
                    else
                    {
                        SB.Append(obfuscators.GetRandomElement());
                    }
                }
                else
                if (!bOnlyObfuscators)
                {
                    SB.Append(c);
                }
            }

            return SB.ToString();
        }

        public static int LevenshteinDistance(string s, string t, bool caseInsensitive = true)
        {
            int n = s.Length;
            int m = t.Length;
            if (caseInsensitive)
            {
                for (int i = 0; i < n; i++)
                {
                    if (char.IsUpper(s[i]))
                    {
                        s = s.ToLower();
                        break;
                    }
                }
                for (int i = 0; i < m; i++)
                {
                    if (char.IsUpper(t[i]))
                    {
                        t = t.ToLower();
                        break;
                    }
                }
            }
            int[,] d = new int[n + 1, m + 1];
            int cost;
            if (n == 0)
            {
                return m;
            }
            if (m == 0)
            {
                return n;
            }
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }
            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    cost = (t[j - 1] == s[i - 1] ? 0 : 1);
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }
            return d[n, m];
        }

        public static string ClosestMatch(IList<string> Options, string Text, bool CaseInsensitive = true)
        {
            if (Options.IsNullOrEmpty())
            {
                return null;
            }

            var result = Options[0];
            var min = LevenshteinDistance(result, Text, CaseInsensitive);
            for (int i = 1, c = Options.Count; i < c; i++)
            {
                var dist = LevenshteinDistance(Options[i], Text, CaseInsensitive);
                if (min > dist)
                {
                    result = Options[i];
                    min = dist;
                }
            }

            return result;
        }

        public static bool ContainsBadWords(string phrase)
        {
            foreach (string s in badWords)
            {
                if (phrase.Contains(s, CompareOptions.IgnoreCase))
                {
                    return true;
                }
            }
            foreach (string s in badWordsExact)
            {
                if (phrase.EqualsNoCase(s))
                {
                    return true;
                }
            }
            return false;
        }

        public static char[] weirdLowerAs = { (char)131, (char)132, (char)133, (char)134, (char)160, (char)166, (char)224, (char)97 };
        public static char[] weirdUpperAs = { (char)65, (char)142, (char)143, (char)146 };
        public static char[] weirdLowerEs = { (char)101, (char)130, (char)136, (char)137, (char)138, (char)238 };
        public static char[] weirdUpperEs = { (char)69, (char)144, (char)228 };
        public static char[] weirdLowerIs = { (char)105, (char)139, (char)140, (char)141, (char)161, (char)173 };
        public static char[] weirdUpperIs = { (char)73, (char)173, (char)179 };
        public static char[] weirdLowerOs = { (char)111, (char)147, (char)148, (char)149, (char)162, (char)229, (char)235, (char)248 };
        public static char[] weirdUpperOs = { (char)79, (char)153, (char)232, (char)233, (char)237 };
        public static char[] weirdLowerUs = { (char)117, (char)150, (char)151, (char)163, (char)230 };
        public static char[] weirdUpperUs = { (char)85, (char)154, (char)230 };

        public static char[] weirdLowerCs = { (char)99, (char)135, (char)155 };
        public static char[] weirdLowerFs = { (char)102, (char)159 };
        public static char[] weirdLowerNs = { (char)110, (char)164, (char)227, (char)239 };
        public static char[] weirdLowerTs = { (char)116, (char)231 };
        public static char[] weirdLowerYs = { (char)121, (char)152 };

        public static char[] weirdUpperBs = { (char)66, (char)225 };
        public static char[] weirdUpperCs = { (char)67, (char)128 };
        public static char[] weirdUpperYs = { (char)89, (char)157 };
        public static char[] weirdUpperLs = { (char)76, (char)156 };
        public static char[] weirdUpperRs = { (char)82, (char)158 };
        public static char[] weirdUpperNs = { (char)78, (char)165, (char)238 };

        static char[] obfuscators = { (char)5, (char)15, (char)22, (char)30, (char)31, (char)178, (char)219, (char)220, (char)221, (char)222, (char)223, (char)248, (char)254 };

        static string[] shortPrepositions = { "at", "by", "in", "of", "on", "to", "up", "from", "with", "into", "over", "out" };
        static string[] prepositions = { "at", "by", "in", "of", "on", "to", "up", "from", "with", "into", "out" };
        static string[] articles = { "an", "a", "the" };
        static string[] demonstrativePronouns = { "this", "that", "those", "these", "such", "none", "neither", "who", "whom", "whose" };
        static string[] conjunctions = { "and", "but", "or", "nor", "for", "as" };
        static string[] ordinalsRoman = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
        static string[] articleStartingWords = { "a", "an", "A", "An", "the", "The", "a", "an", "A", "An", "the", "The", "a", "an", "A", "An", "the", "The", "Some", "some" };
        static string[] badEndingWords = { "this", "were", "every", "are", "which", "their", "has", "your", "that", "who", "our", "additional", "its", "he", "her", "during", "no", "she's", "he's", "than", "they" };
        static string[] badStartingWords = { "were", "though", "them", "him,", "her", "is" };
        static string[] articleExceptions = { "heir", "heiress", "heirloom", "herb", "herbal", "hour", "one", "ubiquitous", "ubiquitously", "ubiquity", "unary", "unicorn", "unicorns", "unicycle", "unidirectional", "unidirectionality", "unidirectionally", "unification", "unifications", "unified", "unifier", "unifies", "uniform", "uniformed", "uniformity", "uniformly", "uniforms", "unify", "unifying", "union", "unionization", "unionize", "unionized", "unionizer", "unionizers", "unionizes", "unionizing", "unions", "uniprocessor", "unique", "uniquely", "uniqueness", "unison", "unit", "unitary", "unities", "uniting", "unity", "univalve", "univalved", "univalves", "universal", "universal", "universality", "universally", "universe", "universes", "universities", "university", "usable", "use", "useless" };
        static string[] badWords = { "nigg", "fag", "nigr", "niggr", "kike" };
        static string[] badWordsExact = { "chikan", "clit", "hamas" };

        private static char[] punctuation = { '.', '!', ',', ':', ';' };

        private static Dictionary<string, string> singularToPlural = new Dictionary<string, string>();
        private static Dictionary<string, string> pluralToSingular = new Dictionary<string, string>();
        private static Dictionary<string, string> irregularPluralization = new Dictionary<string, string>{
            {"atterkop", "atterkoppen"},
            {"attorney general", "attorneys general"},
            {"bergrisi", "bergrisar"},
            {"child", "children"},
            {"childe", "childer"},
            {"commando", "commandos"},
            {"court martial", "courts martial"},
            {"die", "dice"},
            {"djinn", "djinni"},
            {"dunadan", "dunadain"},
            {"eldjotun", "eldjotnar"},
            {"eldthurs", "eldthursar"},
            {"felljotun", "felljotnar"},
            {"fife", "fifes"},
            {"fomor", "fomori"},
            {"foot", "feet"},
            {"forefoot", "forefeet"},
            {"genus", "genera"},
            {"goose", "geese"},
            {"hindfoot", "hindfeet"},
            {"hrimthurs", "hrimthursar"},
            {"ifrit", "ifriti"},
            {"jabberwock", "jabberwocky"},
            {"jerky", "jerkys"},
            {"jotun", "jotnar"},
            {"kin", "kin" },
            {"kindred", "kindred"},
            {"kinsmen", "kinsmen"},
            {"kinswomen", "kinswomen"},
            {"knight templar", "knights templar"},
            {"lb.", "lbs."},
            {"leaf", "leaves"},
            {"loaf", "loaves"},
            {"longstaff", "longstaves"},
            {"mosquito", "mosquitos"},
            {"mouse", "mice"},
            {"notary public", "notaries public"},
            {"octopus", "octopodes"},
            {"opus", "opera"},
            {"ordo", "ordines"},
            {"ox", "oxen"},
            {"pancreas", "pancreata"},
            {"person", "people"},
            {"platypus", "platypoda"},
            {"plus", "plusses"},
            {"quarterstaff", "quarterstaves"},
            {"rhinox", "rhinoxen"},
            {"risi", "risar"},
            {"secretary general", "secretaries general"},
            {"shaman", "shamans"},
            {"staff", "staves"},
            {"sturmjotun", "sturmjotnar"},
            {"surgeon general", "surgeons general"},
            {"talisman", "talismans"},
            {"thief", "thieves"},
            {"tooth", "teeth"},
            {"topaz", "topazes"},
            {"townsperson", "townspeople"},
            {"moment in time chosen arbitrarily", "moments in time chosen arbitrarily"}
        };
        private static string[] identicalPluralization = { "barracks", "bison", "buffalo", "caribou", "chitin", "chosen", "corps", "deer", "einheriar", "fish", "fruit", "geisha", "gi", "hellspawn", "katana", "kraken", "lamia", "kris", "means", "moose", "naga", "ninja", "nunchaku", "oni", "remains", "sai", "scissors", "series", "sheep", "shrimp", "shuriken", "spawn", "species", "sputum", "waterworks", "wakizashi", "yeti", "yoroi", "young", "pentaceps" };
        private static string[] latinPluralization = { "abacus", "adytum", "alkalus", "alumnus", "alumno", "alumna", "anima", "animo", "animus", "antenna", "apex", "appendix", "arboretum", "astrum", "automaton", "axis", "bacterium", "ballista", "cacosteum", "cactus", "cestus", "cinctus", "cognomen", "corpus", "datum", "desideratum", "dictum", "dominatrix", "drosophilium", "ellipsis", "emerita", "emerito", "emeritus", "epona", "eques", "equus", "erratum", "esophagus", "exoculus", "exodus", "fascia", "focus", "forum", "fungus", "haruspex", "hippocampus", "hippopotamus", "hypha", "iambus", "illuminata", "illuminato", "illuminatus", "imperator", "imperatrix", "incarnus", "larva", "locus", "lorica", "maga", "mago", "magus", "manica", "matrix", "medium", "melia", "momentum", "neurosis", "nexus", "nomen", "nucleus", "patagium", "pegasus", "penis", "persona", "phenomenon", "phoenix", "pilum", "plexus", "praenomen", "psychosis", "quantum", "radius", "rectum", "sanctum", "scintilla", "scriptorium", "scrotum", "scutum", "septum", "simulacrum", "stratum", "substratum", "testis", "tympani", "ultimatum", "uterus", "vagina", "vertex", "vomitorium", "vortex", "vulva" };
        private static string[] greekPluralization1 = { "archon", "aristos", "astron", "bebelos", "charisma", "chimera", "daimon", "domos", "echthros", "eidolon", "ephemeris", "epopis", "hegemon", "horos", "hystrix", "kentaur", "kharisma", "kudos", "laryngis", "larynx", "lemma", "logos", "mestor", "minotaur", "mnemon", "mythos", "omphalos", "ouros", "patris", "pharynx", "pragma", "rhetor", "rhinoceros", "schema", "stigma", "telos", "topos" };
        private static string[] greekPluralization2 = { "diokesis", "ganglion", "noumenon", "numenon", "praxis", "therion" };
        private static string[] hebrewPluralization = { "aswad", "chaya", "cherub", "galgal", "golem", "kabbalah", "keruv", "khaya", "nefesh", "nephil", "neshamah", "qabalah", "ruach", "ruakh", "sephirah", "seraph", "yechida", "yekhida" };
        private static string[] dualPluralization = { "emerita", "emeritus" };

        private static Dictionary<string, string> firstPersonToThirdPerson = new Dictionary<string, string>();
        private static Dictionary<string, string> firstPersonToThirdPersonWithSpace = new Dictionary<string, string>();
        private static Dictionary<string, string> thirdPersonToFirstPerson = new Dictionary<string, string>();

        private static Dictionary<string, string> irregularThirdPerson = new Dictionary<string, string>{
            {"'re", "'s"},
            {"'ve", "'s"},
            {"are", "is"},
            {"aren't", "isn't"},
            {"cannot", "cannot"},
            {"can't", "can't"},
            {"caught", "caught"},
            {"could", "could"},
            {"couldn't", "couldn't"},
            {"don't", "doesn't"},
            {"grew", "grew"},
            {"had", "had"},
            {"have", "has"},
            {"may", "may"},
            {"might", "might"},
            {"must", "must"},
            {"shall", "shall"},
            {"shouldn't", "shouldn't"},
            {"should", "should"},
            {"sought", "sought"},
            {"were", "was"},
            {"will", "will"},
            {"won't", "won't"},
            {"wouldn't", "wouldn't"},
            {"would", "would"},
        };

        private static StringBuilder SB1 = new(512);
        private static StringBuilder SB2 = new(32);

    }
}
