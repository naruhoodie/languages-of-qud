using System;
using XRL;
using System.Globalization;
using XRL.World;

namespace XRL.Language
{
    [Translator.LanguageProvider("ja")]
    public class TranslatorJapanese : TranslatorBase
    {
        private const char  HIGH_SURROGATE_START  = '\ud800';
        private const char  HIGH_SURROGATE_END    = '\udbff';
        private const char  LOW_SURROGATE_START   = '\udc00';
        private const char  LOW_SURROGATE_END     = '\udfff';

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

    }

}