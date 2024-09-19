using System;
using Microsoft.Extensions.DependencyInjection;

namespace InstrumentationAmplifier.Utils;

public class LazyService<T> : Lazy<T> where T : class
{
    public LazyService(IServiceProvider serviceProvider)
        : base(serviceProvider.GetRequiredService<T>) { }
}