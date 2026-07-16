using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace jeo_ano_ba.ViewModels;

public class BaseViewModel : INotifyPropertyChanged
{
    //Event required by INotifyPropertyChanged, notify UI thet a property value changed
    public event PropertyChangedEventHandler? PropertyChanged;
    //manually asks of 
    protected void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        //passes to the property that changed
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
    //updates property's value cbecks if it changed and updates UI automatically
    //true if value changed, false if not
    protected bool SetProperty<T>(
        ref T backingStore,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        //stops UI redrawing
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;
        //iverwrites old value in the backing field with new value
        backingStore = value;
        //tells the ui that this specific property looks new
        OnPropertyChanged(propertyName);
        return true;
    }
}