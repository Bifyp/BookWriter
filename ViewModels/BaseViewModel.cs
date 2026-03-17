using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BookWriter.ViewModels
{
    /// <summary>
    /// Base class for all ViewModels.
    /// Provides INotifyPropertyChanged, SetProperty helper,
    /// and a thread-safe dispatcher helper for async operations.
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            // Always dispatch to UI thread — safe to call from background threads
            if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == false)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(
                    () => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
            }
            else
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected bool SetProperty<T>(ref T field, T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// SetProperty with optional callback when value actually changes.
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, Action onChanged,
            [CallerMemberName] string? propertyName = null)
        {
            if (!SetProperty(ref field, value, propertyName)) return false;
            onChanged();
            return true;
        }

        protected void RaisePropertiesChanged(params string[] propertyNames)
        {
            foreach (var name in propertyNames)
                OnPropertyChanged(name);
        }
    }
}
