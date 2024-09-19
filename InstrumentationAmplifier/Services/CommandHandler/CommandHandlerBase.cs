using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace InstrumentationAmplifier.Services.CommandHandler;

public abstract class CommandHandlerBase<TResponse>
{
    protected readonly SortedList<string, CommandHandler> Handlers = new();

    protected CommandHandlerBase()
    {
        var instanceConstant = Expression.Constant(this);

        var classMethods = this.GetType().GetMethods(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod);
        
        foreach (var method in classMethods)
        {
            var attributes = method.GetCustomAttributes(typeof(CommandAttribute), false);
            foreach (object attribute in attributes)
            {
                AddCommand((CommandAttribute)attribute, method);
            }
        }

        void AddCommand(CommandAttribute attribute, MethodInfo methodInfo)
        {
            bool hasStringParam = methodInfo.GetParameters().Length == 1;

            var parameter = Expression.Parameter(typeof(string), "command");
            var call = hasStringParam
                ? Expression.Call(instanceConstant, methodInfo, parameter)
                : Expression.Call(instanceConstant, methodInfo);

            Func<String, TResponse> expression =
                Expression.Lambda<Func<string, TResponse>>(call, parameter).Compile();

            Handlers.Add(attribute.StartWith, new(expression, attribute.TrimStart));
        }
    }

    protected bool TryHandle(string message, [NotNullWhen(true)] out TResponse? response)
    {
        KeyValuePair<String, CommandHandler>? handler = Handlers
            .Where(h => message.StartsWith(h.Key))
            .Select(h => new KeyValuePair<String, CommandHandler>?(h))
            .MaxBy(h => h!.Value.Key.Length);

        if (handler is { } v)
        {
            string trimmedMessage = v.Value.TrimStart ? message.Remove(0, v.Key.Length) : message;
            response = v.Value.Handler(trimmedMessage);
            return true;
        }

        response = default;
        return false;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    protected class CommandAttribute : Attribute
    {
        [SetsRequiredMembers] public CommandAttribute(String startWith) => StartWith = startWith;
        
        public string StartWith { get; }
        public bool TrimStart { get; init; } = true;
    }

    protected record CommandHandler(Func<string, TResponse> Handler, bool TrimStart);
}