using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using AVR.Debugger.Interfaces.Models;
using CPUView.Annotations;

namespace CPUView
{
    public class RegistersViewModel : INotifyPropertyChanged
    {
        private bool _inDebug;
        public List<Register> Registers { get; set; }

        public bool InDebug
        {
            get { return _inDebug; }
            set
            {
                if (value == _inDebug) return;
                _inDebug = value;
                OnPropertyChanged();
            }
        }

        public RegistersViewModel()
        {
            Registers = new List<Register>();
            Registers.Add(new Register() {RegisterName = "PC", Value = 0, Size = 16});
            Registers.Add(new Register() { RegisterName = "Stack", Value = 0, Size = 16 });
            for (int i = 0; i < 32; i++)
            {
                Registers.Add(new Register() { RegisterName = $"R{i}", Value = 0, Size = 8 });
            }
        }

        public void UpdateCpuState(CpuState state)
        {
            Registers.First(r => r.RegisterName == "PC").Value = state.PC;
            Registers.Find(r => r.RegisterName == "Stack").Value = state.Stack;
            for (int i = 0; i < 32; i++)
            {
                Registers.Find(r => r.RegisterName == $"R{i}").Value = state.Registers[i];
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
