using System;
using Avalonia.Controls;

namespace InstrumentationAmplifier.Common;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ViewAttribute<TView> : Attribute where TView : Control {}