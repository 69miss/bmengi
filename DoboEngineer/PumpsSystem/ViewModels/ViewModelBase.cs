using CommunityToolkit.Mvvm.ComponentModel;
using PumpsSystem.Module;

namespace PumpsSystem.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        public PumpLang L { get; set; } = new PumpEnLang();
    }
}
