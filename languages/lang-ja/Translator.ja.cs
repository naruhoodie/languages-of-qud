using System;
using XRL;

namespace XRL.Language
{
    [Translator.LanguageProvider("ja")]
    public class TranslatorJapanese : TranslatorBase
    {
        private char[] DontBreakAfter = {
            // Don't break after color control codes.
            '&', '^',
            // starting punctuation
            '(', '[', '｛', '〔', '〈', '《', '「', '『', '【', '〘', '〖', '〝', '\'', '"', '｟', '«' };
        private char[] DontBreakBefore = {
            // small kana and other modifiers
            'ァ', 'ィ', 'ゥ', 'ェ', 'ォ', 'ッ', 'ャ', 'ュ', 'ョ', 'ヮ', 'ヵ', 'ヶ', 'ぁ', 'ぃ', 'ぅ', 'ぇ', 'ぉ', 'っ', 'ゃ', 'ゅ', 'ょ', 'ゎ', 'ゕ', 'ゖ', 'ㇰ', 'ㇱ', 'ㇲ', 'ㇳ', 'ㇴ', 'ㇵ', 'ㇶ', 'ㇷ', 'ㇸ', 'ㇹ', 'ㇺ', 'ㇻ', 'ㇼ', 'ㇽ', 'ㇾ', 'ㇿ', '々', '〻', 'ヽ', 'ヾ', 'ー', '゙', '゚',
            // closing brackets/quotes
            ')', ']', '｝', '〕', '〉', '》', '」', '』', '】', '〙', '〗', '〟', '\'', '"', '｠', '»',
            // hyphens
            '‐', '゠', '–', '〜',
            // delimiters and non-starting sentence punctuation
            '？', '!', '‼', '⁇', '⁈', '⁉', '・', '、', ':', ';', ',', '.', '。', '、' };
        private bool CanBreakBefore(char @char)
        {
            if (@char == ' ' || @char == '\n') return true;
            if (@char <= 255) return false;
            return !DontBreakBefore.Contains(@char);
        }
        private bool CanBreakAfter(char @char) => !DontBreakAfter.Contains(@char);
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
                BreakBeforeIndex++;
            }
            ReplaceIfBroken = BreakBeforeIndex < Span.Length && (Span[BreakBeforeIndex] == ' ' || Span[BreakBeforeIndex] == '\n');
        }

    }

}