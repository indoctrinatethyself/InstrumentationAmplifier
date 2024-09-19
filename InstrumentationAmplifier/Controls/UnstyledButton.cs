using System;
using Avalonia.Controls;

namespace InstrumentationAmplifier.Controls;

public class UnstyledButton : Button
{
    protected override Type StyleKeyOverride => typeof(ContentControl);
}