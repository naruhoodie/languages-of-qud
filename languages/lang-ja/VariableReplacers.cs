using XRL.World;
using XRL.World.Text.Attributes;
using XRL.World.Text.Delegates;

namespace LangauagesOfQud
{
    [HasVariableReplacer]
    public static class VariableReplacers
    {
        [VariableObjectReplacer("届")]
        public static string TodoReplacer(DelegateContext Context)
        {
            if (Context.Target is GameObject target)
            {
                return "[届\\TODO]" + target.GetDisplayName();
            }
            return "[届\\TODO]" + Context.Explicit;
        }
    }
}