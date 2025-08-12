using System;
using XRL;
using System.Globalization;
using Cysharp.Text;
using XRL.World;
using XRL.World.Text;
using System.Text;
using System.Collections.Generic;

namespace XRL.Language
{
    [LanguageProvider("ja")]
    public class TranslatorJapanese : TranslatorBase
    {
        private const char HIGH_SURROGATE_START = '\ud800';
        private const char HIGH_SURROGATE_END = '\udbff';
        private const char LOW_SURROGATE_START = '\udc00';
        private const char LOW_SURROGATE_END = '\udfff';

        private char[] DontBreakAfter = {
            // Don't break after color control codes.
            '&', '^',
            // or simple quotes
            '\'', '"'
        };
        private char[] DontBreakBefore = {
            // small kana and other modifiers
            'ァ', 'ィ', 'ゥ', 'ェ', 'ォ', 'ッ', 'ャ', 'ュ', 'ョ', 'ヮ', 'ヵ', 'ヶ', 'ぁ', 'ぃ', 'ぅ', 'ぇ', 'ぉ', 'っ', 'ゃ', 'ゅ', 'ょ', 'ゎ', 'ゕ', 'ゖ', 'ㇰ', 'ㇱ', 'ㇲ', 'ㇳ', 'ㇴ', 'ㇵ', 'ㇶ', 'ㇷ', 'ㇸ', 'ㇹ', 'ㇺ', 'ㇻ', 'ㇼ', 'ㇽ', 'ㇾ', 'ㇿ', '々', '〻', 'ヽ', 'ヾ', 'ー', '゛', '゜', 'ゝ', 'ゞ',
            // hyphens
            '‐', '゠', '–', '〜',
            // delimiters and non-starting sentence punctuation
            '？', '!', '‼', '⁇', '⁈', '⁉', '・', '、', ':', ';', ',', '.', '。', '、',
            // simple quotes
            '\'', '"'
        };
        private bool CanBreakBefore(char @char)
        {
            if (@char == ' ' || @char == '\n') return true;
            if (@char <= 255) return false;
            if (@char >= LOW_SURROGATE_START && @char <= LOW_SURROGATE_END)
                return false;
            var uc = CharUnicodeInfo.GetUnicodeCategory(@char);
            if (uc == UnicodeCategory.NonSpacingMark ||
                uc == UnicodeCategory.SpacingCombiningMark ||
                uc == UnicodeCategory.EnclosingMark ||
                uc == UnicodeCategory.ClosePunctuation)
                return false;
            return !DontBreakBefore.Contains(@char);
        }
        private bool CanBreakAfter(char @char)
        {
            if (@char >= HIGH_SURROGATE_START && @char <= HIGH_SURROGATE_END)
                return false;
            if (CharUnicodeInfo.GetUnicodeCategory(@char) == UnicodeCategory.OpenPunctuation) return false;
            return !DontBreakAfter.Contains(@char);
        }

        public override void NextPossibleLineBreakIndex(ReadOnlySpan<char> Span, int StartIndex, out int BreakBeforeIndex, out bool ReplaceIfBroken)
        {
            BreakBeforeIndex = StartIndex + 1;
            while (BreakBeforeIndex < Span.Length && !(CanBreakBefore(Span[BreakBeforeIndex]) && CanBreakAfter(Span[BreakBeforeIndex - 1])))
            {
                BreakBeforeIndex++;
            }
            // keep the ruby blob together
            if (Span[BreakBeforeIndex - 1] == '[')
            {
                while (BreakBeforeIndex < Span.Length && Span[BreakBeforeIndex] != ']')
                {
                    BreakBeforeIndex++;
                }
                if (BreakBeforeIndex < Span.Length) BreakBeforeIndex++;
            }
            ReplaceIfBroken = BreakBeforeIndex < Span.Length && (Span[BreakBeforeIndex] == ' ' || Span[BreakBeforeIndex] == '\n');
        }

        public override DescriptionBuilder CreateDescriptionBuilder(int Cutoff = int.MaxValue, bool BaseOnly = false)
        {
            return new DescriptionBuilderJa { Cutoff = int.MaxValue, BaseOnly = BaseOnly };
        }

        /**
         * support method for Cardinal() and Ordinal()
         */
        private static void ProcessMagnitude(ref long num, ref int magnitude, TextBuilder SB, string place)
        {
            if (magnitude > 4)
            {
                magnitude -= magnitude % 4;
            }
            long offset = (long)Math.Floor(Math.Exp(magnitude * Math.Log(10)));
            long remainder = num % offset;
            long val = num - remainder;
            long count = val / offset;
            if (count > 0)
            {
                SB.Append(Cardinal(count));
                SB.Append(place);
                num = remainder;
            }
            magnitude--;
        }

        /**
         * support method for Cardinal() and Ordinal()
         */
        private static bool ProcessMagnitudes(ref long num, ref int magnitude, TextBuilder SB, string suffix = null)
        {
            switch (magnitude)
            {
                case 20:
                    ProcessMagnitude(ref num, ref magnitude, SB, "垓");
                    if (num == 0)
                    {
                        if (suffix != null)
                        {
                            SB.Append(suffix);
                        }
                        return true;
                    }
                    goto case 16;
                case 19:
                case 18:
                case 17:
                case 16:
                    ProcessMagnitude(ref num, ref magnitude, SB, "京");
                    if (num == 0)
                    {
                        if (suffix != null)
                        {
                            SB.Append(suffix);
                        }
                        return true;
                    }
                    goto case 12;
                case 15:
                case 14:
                case 13:
                case 12:
                    ProcessMagnitude(ref num, ref magnitude, SB, "兆");
                    if (num == 0)
                    {
                        if (suffix != null)
                        {
                            SB.Append(suffix);
                        }
                        return true;
                    }
                    goto case 8;
                case 11:
                case 10:
                case 9:
                case 8:
                    ProcessMagnitude(ref num, ref magnitude, SB, "億");
                    if (num == 0)
                    {
                        if (suffix != null)
                        {
                            SB.Append(suffix);
                        }
                        return true;
                    }
                    goto case 4;
                case 7:
                case 6:
                case 5:
                case 4:
                    ProcessMagnitude(ref num, ref magnitude, SB, "万");
                    if (num == 0)
                    {
                        if (suffix != null)
                        {
                            SB.Append(suffix);
                        }
                        return true;
                    }
                    goto case 3;
                case 3:
                    ProcessMagnitude(ref num, ref magnitude, SB, "千");
                    if (num == 0)
                    {
                        if (suffix != null)
                        {
                            SB.Append(suffix);
                        }
                        return true;
                    }
                    goto case 2;
                case 2:
                    ProcessMagnitude(ref num, ref magnitude, SB, "百");
                    if (num == 0)
                    {
                        if (suffix != null)
                        {
                            SB.Append(suffix);
                        }
                        return true;
                    }
                    goto case 1;
                case 1:
                    if (magnitude > 0)
                    {
                        ProcessMagnitude(ref num, ref magnitude, SB, "十");
                        if (num == 0)
                        {
                            if (suffix != null)
                            {
                                SB.Append(suffix);
                            }
                            return true;
                        }
                    }
                    break;
            }
            return false;
        }

        public static string Cardinal(int num)
        {
            if (num == 0)
            {
                return "零";
            }
            return Cardinal((long)num);
        }
        public static string Cardinal(long num)
        {
            if (num == 0)
            {
                return "零";
            }
            using var SB = TextBuilder.Get();
            if (num < 0)
            {
                SB.Append("マイナス");
                num = -num;
            }
            int magnitude = (int)Math.Floor(Math.Log10(num));
            ProcessMagnitudes(ref num, ref magnitude, SB);
            switch (num)
            {
                case 1: SB.Append("一"); break;
                case 2: SB.Append("二"); break;
                case 3: SB.Append("三"); break;
                case 4: SB.Append("四"); break;
                case 5: SB.Append("五"); break;
                case 6: SB.Append("六"); break;
                case 7: SB.Append("七"); break;
                case 8: SB.Append("八"); break;
                case 9: SB.Append("九"); break;
            }
            return SB.ToString();
        }

    }

    public static class TranslatorJapaneseExtensions
    {
        /// <summary>
        /// Converts an "adjective" to て-form.
        /// な-adjectives: replace with で
        /// い-adjectives: replace with くて
        /// の-adjectives: no change
        /// </summary>
        public static void GetAdjectiveてForm(TextBuilder Adjective)
        {
            //TODO: need to handle Color Formatting!
            if (Adjective.EndsWith("い"))
            {
                Adjective.Remove(Adjective.Length - 1, 1).Append("くて");
            }
            else if (Adjective.EndsWith("な"))
            {
                Adjective.Remove(Adjective.Length - 1, 1).Append("で");
            }
        }

        /// <summary>
        /// Creates an adjective "and" list
        /// </summary>
        /// <param name="List"></param>
        /// <returns></returns>
        public static void AppendてList(this TextBuilder SB, IReadOnlyList<string> List)
        {
            if (List.Count == 0)
            {
                return;
            }
            for (int i = 0, j = List.Count - 1; i < j; i++)
            {
                SB.Append(List[i]);
                GetAdjectiveてForm(SB);
            }
            SB.Append(List[^1]);
        }

    }

}