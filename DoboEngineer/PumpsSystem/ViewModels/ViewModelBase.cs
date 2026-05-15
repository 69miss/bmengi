using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using PumpsSystem.Module;
using System.Globalization;

namespace PumpsSystem.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        public ViewModelBase() {
            WeakReferenceMessenger.Default.Register<PumpLang>(this, (p, p1) => { L = p1; });

            if (CultureInfo.CurrentCulture.Name == "en")
            {
               
            }
            else
            {
               L=PumpLang.Instance;
            }
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
