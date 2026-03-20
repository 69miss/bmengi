using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using PumpsSystem.Module;

namespace PumpsSystem.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        public ViewModelBase() {
            WeakReferenceMessenger.Default.Register<PumpLang>(this, (p, p1) => { L = p1; });
        }
        public PumpLang L
        {
            get; set
            {
                field = value;
                OnPropertyChanged();
            }
        }=  PumpEnLang.Instance;

    }
}
