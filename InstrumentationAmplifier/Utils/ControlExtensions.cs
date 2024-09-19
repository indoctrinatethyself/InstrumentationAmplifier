using ControlBase = Avalonia.StyledElement;

namespace InstrumentationAmplifier.Utils;

public static class ControlExtensions
{
    public static TParent? GetParent<TParent>(this ControlBase? child)
        where TParent : ControlBase
    {
        do
        {
            if (child is TParent parent) return parent;
            child = child!.Parent;
        }
        while (child != null);

        return null;
    }

    public static bool IsChildOf(this ControlBase? child, ControlBase parent)
    {
        do
        {
            if (child == parent) return true;
            child = child!.Parent;
        }
        while (child != null);

        return false;
    }
}