using System;
using System.Threading;
using System.Threading.Tasks;

namespace InstrumentationAmplifier.Services;

public interface IToastService
{
    Task ShowToastAsync(Common.ToastViewModelBase toastViewModel, TimeSpan duration,
        CancellationToken cancellationToken = default);
}