// XRL.Language.GetText static interfaces:

// Static strings
_S("Static string");
// if passed two arguments, the first is used as "prepended" context to the string in the lookup table
_S(nameof(Options)+".static ID", "Static String");
// advanced replacers that can take 'n' game objects with names, and various text bits, using GameTextVariableReplace/etc
_T("Using =subject.t= replacers,etc")
    .AddSubject(SomeGameObject)
    .ToString();
// XML Style templates loaded out of various XML places
_X("XML Template ID")
    .CollectStats(someCollectMethod)
    .ToString();

// internally, this would do _S("verb", "are") and _S("preposition", "blown back by")
// -or- potentially, could be it's own table with _T("XDidYToZ.are.blown back by")
XDidYToZ(grabbed, "are", "blown back by", ParentObject, ColorAsBadFor: grabbed);
----
