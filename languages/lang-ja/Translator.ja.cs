using System;
using XRL;

namespace XRL.Language
{
    [Translator.LanguageProvider("ja")]
    public class TranslatorJapanese : TranslatorBase
    {
        // Don't break after color control codes.
        private char[] DontBreakAfter = { '&', '^' };
        private char[] DontBreakBefore = { '。', '、' };
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
            if (Span[BreakBeforeIndex-1] == '[')
            {
                while (BreakBeforeIndex < Span.Length && Span[BreakBeforeIndex] != ']')
                {
                    BreakBeforeIndex++;
                }
            }
            ReplaceIfBroken = BreakBeforeIndex < Span.Length && (Span[BreakBeforeIndex] == ' ' || Span[BreakBeforeIndex] == '\n');
        }

    }

}