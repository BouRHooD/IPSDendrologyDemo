using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace IPSDendrologyDemo.Other
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            try
            {
                var handler_Property = PropertyChanged;
                handler_Property?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
            }
        }

        public event EventHandler OnRequestClose;
        protected void RaiseCloseRequest() => OnRequestClose?.Invoke(this, EventArgs.Empty);
    }
}
