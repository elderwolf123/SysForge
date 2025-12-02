using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace RamOptimizerUI.MVVM
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private object _currentView;

        public object CurrentView
        {
            get { return _currentView; }
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public ICommand NavigateCommand { get; }

        public MainViewModel()
        {
            NavigateCommand = new RelayCommand(Navigate);
            CurrentView = new Views.DashboardView();
        }

        private void Navigate(object destination)
        {
            switch (destination?.ToString())
            {
                case "Dashboard":
                    CurrentView = new Views.DashboardView();
                    break;
                case "Processes":
                    CurrentView = new Views.ProcessView();
                    break;
                case "Performance":
                    CurrentView = new Views.PerformanceView();
                    break;
                case "Compression":
                    CurrentView = new Views.CompressionView();
                    break;
                case "Settings":
                    CurrentView = new Views.SettingsView();
                    break;
                case "PowerProfiles":
                    CurrentView = new Views.PowerProfileView();
                    break;
                default:
                    CurrentView = new Views.DashboardView();
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
