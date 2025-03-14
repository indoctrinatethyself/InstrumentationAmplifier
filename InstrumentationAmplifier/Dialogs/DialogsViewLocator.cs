using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using InstrumentationAmplifier.Common;

namespace InstrumentationAmplifier.Dialogs;

public class DialogsViewLocator : IDataTemplate
{
    private readonly Dictionary<Type, Type> _registrations = new();

    private string? _dialogsNamespace;
    private string DialogsNamespace => _dialogsNamespace ??= typeof(DialogsViewLocator).Namespace!;

    public Control Build(object? data)
    {
        Type vmType = data!.GetType();

        _registrations.TryGetValue(vmType, out Type? viewType);
        if (viewType == null)
        {
            var attribute = vmType.GetCustomAttribute(typeof(ViewAttribute<>), false);
            if (attribute != null)
                viewType = attribute.GetType().GetGenericArguments()[0];
        }

        if (viewType == null)
        {
            var name = DialogsNamespace + '.' + vmType.Name.Replace("ViewModel", "View");
            viewType = Type.GetType(name);
        }

        if (viewType != null)
            return (Control)Activator.CreateInstance(viewType)!;

        return new TextBlock { Text = "Not found dialog view for view model: " + vmType.FullName };
    }

    public bool Match(object? data) => data is DialogViewModelBase;

    public DialogsViewLocator Register<TViewModel, TView>()
        where TViewModel : DialogViewModelBase
        where TView : Control, new()
    {
        _registrations.Add(typeof(TViewModel), typeof(TView));
        return this;
    }
}