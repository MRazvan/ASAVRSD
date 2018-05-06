using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AVR.Debugger.SerialViewer
{
    internal class SerialViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Content { get; set; }

        public void ShowCharacter(char v)
        {
            Content += v;
            OnPropertyChanged("Content");
        }
    }
}