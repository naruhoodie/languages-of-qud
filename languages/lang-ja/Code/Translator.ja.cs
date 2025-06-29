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
    [Translator.LanguageProvider("ja")]
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
        private static void ProcessMagnitude(ref int num, ref int magnitude, TextBuilder result, string place)
        {
            if (magnitude > 4)
            {
                magnitude -= magnitude % 4;
            }
            int offset = (int)Math.Floor(Math.Exp(magnitude * Math.Log(10)));
            int remainder = num % offset;
            int val = num - remainder;
            int count = val / offset;
            if (count > 0)
            {
                result.Append(Cardinal(count));
                result.Append(place);
                num = remainder;
            }
            magnitude--;
        }

        /**
         * support method for Cardinal() and Ordinal()
         */
        private static void ProcessMagnitude(ref long num, ref int magnitude, TextBuilder result, string place)
        {
            if (magnitude > 4)
            {
                magnitude -= magnitude % 4;
            }
            int offset = (int)Math.Floor(Math.Exp(magnitude * Math.Log(10)));
            long remainder = num % offset;
            long val = num - remainder;
            long count = val / offset;
            if (count > 0)
            {
                result.Append(Cardinal(count));
                result.Append(place);
                num = remainder;
            }
            magnitude--;
        }

        /**
         * support method for Cardinal() and Ordinal()
         */
        private static bool ProcessMagnitudes(ref int num, ref int magnitude, TextBuilder result, string suffix = null)
        {
            switch (magnitude)
            {
                case 20:
                    ProcessMagnitude(ref num, ref magnitude, result, "垓");
                    if (num == 0)
                    {
                        if (suffix != null)
                        {
                            result.Append(suffix);
                        }
                        return true;
                    }
                    goto case 16;
                case 19:
                case 18:
                case 17:
                case 16:
                    ProcessMagnitude(ref num, ref magnitude, result, "京");
                    if (num == 0)
                    {
                        if (suffix != null)
                        {
                            result.Append(suffix);
                        }
                        return true;
                    }
                    goto case 12;
                case 15:
                case 14:
                case 13:
                case 12:
                    ProcessMagnitude(ref num, ref magnitude, result, "兆");
                    if (num == 0)
                    {
                        if (suffix != null)
                        {
                            result.Append(suffix);
                        }
                        return true;
                    }
                    goto case 8;
                case 11:
                case 10:
                case 9:
                case 8:
                    ProcessMagnitude(ref num, ref magnitude, result, "億");
                    if (num == 0)
                    {
                        if (suffix != null)
                        {
                            result.Append(suffix);
                        }
                        return true;
                    }
                    goto case 4;
                case 7:
                case 6:
                case 5:
                case 4:
                    ProcessMagnitude(ref num, ref magnitude, result, "万");
                    if (num == 0)
                    {
                        if (suffix != null)
                        {
                            result.Append(suffix);
                        }
                        return true;
                    }
                    goto case 3;
                case 3:
                    ProcessMagnitude(ref num, ref magnitude, result, "千");
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
                    ProcessMagnitude(ref num, ref magnitude, result, "百");
                    if (num == 0)
                    {
                        if (suffix != null)
                        {
                            result.Append(suffix);
                        }
                        return true;
                    }
                    goto case 1;
                case 1:
                    if (magnitude > 0)
                    {
                        ProcessMagnitude(ref num, ref magnitude, result, "十");
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
        private static bool ProcessMagnitudes(ref long num, ref int magnitude, TextBuilder result, string suffix = null)
        {
            switch (magnitude)
            {
                case 20:
                    ProcessMagnitude(ref num, ref magnitude, result, "垓");
                    if (num == 0)
                    {
                        if (suffix != null)
                        {
                            result.Append(suffix);
                        }
                        return true;
                    }
                    goto case 16;
                case 19:
                case 18:
                case 17:
                case 16:
                    ProcessMagnitude(ref num, ref magnitude, result, "京");
                    if (num == 0)
                    {
                        if (suffix != null)
                        {
                            result.Append(suffix);
                        }
                        return true;
                    }
                    goto case 12;
                case 15:
                case 14:
                case 13:
                case 12:
                    ProcessMagnitude(ref num, ref magnitude, result, "兆");
                    if (num == 0)
                    {
                        if (suffix != null)
                        {
                            result.Append(suffix);
                        }
                        return true;
                    }
                    goto case 8;
                case 11:
                case 10:
                case 9:
                case 8:
                    ProcessMagnitude(ref num, ref magnitude, result, "億");
                    if (num == 0)
                    {
                        if (suffix != null)
                        {
                            result.Append(suffix);
                        }
                        return true;
                    }
                    goto case 4;
                case 7:
                case 6:
                case 5:
                case 4:
                    ProcessMagnitude(ref num, ref magnitude, result, "万");
                    if (num == 0)
                    {
                        if (suffix != null)
                        {
                            result.Append(suffix);
                        }
                        return true;
                    }
                    goto case 3;
                case 3:
                    ProcessMagnitude(ref num, ref magnitude, result, "千");
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
                    ProcessMagnitude(ref num, ref magnitude, result, "百");
                    if (num == 0)
                    {
                        if (suffix != null)
                        {
                            result.Append(suffix);
                        }
                        return true;
                    }
                    goto case 1;
                case 1:
                    if (magnitude > 0)
                    {
                        ProcessMagnitude(ref num, ref magnitude, result, "十");
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

        public static string Cardinal(int num)
        {
            if (num == 0)
            {
                return "零";
            }
            using var result = TextBuilder.Get();
            if (num < 0)
            {
                result.Append("マイナス");
                num = -num;
            }
            int magnitude = (int)Math.Floor(Math.Log10(num));
            ProcessMagnitudes(ref num, ref magnitude, result);
            switch (num)
            {
                case 1: result.Append("一"); break;
                case 2: result.Append("二"); break;
                case 3: result.Append("三"); break;
                case 4: result.Append("四"); break;
                case 5: result.Append("五"); break;
                case 6: result.Append("六"); break;
                case 7: result.Append("七"); break;
                case 8: result.Append("八"); break;
                case 9: result.Append("九"); break;
            }
            return result.ToString();
        }
        public static string Cardinal(long num)
        {
            if (num == 0)
            {
                return "零";
            }
            using var result = TextBuilder.Get();
            if (num < 0)
            {
                result.Append("マイナス");
                num = -num;
            }
            int magnitude = (int)Math.Floor(Math.Log10(num));
            ProcessMagnitudes(ref num, ref magnitude, result);
            switch (num)
            {
                case 1: result.Append("一"); break;
                case 2: result.Append("二"); break;
                case 3: result.Append("三"); break;
                case 4: result.Append("四"); break;
                case 5: result.Append("五"); break;
                case 6: result.Append("六"); break;
                case 7: result.Append("七"); break;
                case 8: result.Append("八"); break;
                case 9: result.Append("九"); break;
            }
            return result.ToString();
        }

        public static string GetAdjectiveてForm(string Adjective)
        {
            using var SB = ZString.CreateStringBuilder();
            if (Adjective.EndsWith("い"))
            {
                SB.Append(Adjective.Substring(0, Adjective.Length - 1));
                SB.Append("くて");
            }
            else if (Adjective.EndsWith("な"))
            {
                SB.Append(Adjective.Substring(0, Adjective.Length - 1));
                SB.Append("で");
            }
            else
            {
                SB.Append(Adjective);
            }
            return SB.ToString();
        }

        public static string MakeてList(IReadOnlyList<string> List)
        {
            if (List.Count == 0)
            {
                return "";
            }
            if (List.Count == 1)
            {
                return List[0];
            }
            using var SB = ZString.CreateStringBuilder();
            for (int i = 0, j = List.Count - 1; i < j; i++)
            {
                SB.Append(GetAdjectiveてForm(List[i]));
            }
            SB.Append(List[List.Count - 1]);
            return SB.ToString();
        }

    } 

}