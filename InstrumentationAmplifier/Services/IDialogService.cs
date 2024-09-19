using System.Threading;
using System.Threading.Tasks;

namespace InstrumentationAmplifier.Services;

public interface IDialogService
{
    Task<bool> ShowDialogAsync(Common.DialogViewModelBase dialogViewModel, CancellationToken cancellationToken = default);
}