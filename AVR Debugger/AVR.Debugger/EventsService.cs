using AVR.Debugger.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AVR.Debugger
{
    internal class EventsService : IEventService
    {
        
        private readonly Dictionary<Events, List<Action<object>>> _eventHandlers;
        private readonly Dictionary<Events, List<Action>> _eventHandlersNoParams;
        private ISynchronizeInvoke _syncronizeInvoke;

        internal EventsService(ISynchronizeInvoke synchronizeInvoke)
        {
            _eventHandlers = new Dictionary<Events, List<Action<object>>>();
            _eventHandlersNoParams = new Dictionary<Events, List<Action>>();
            _syncronizeInvoke = synchronizeInvoke;
        }

        public void AddEventHandler(Events key, Action action)
        {
            if (!_eventHandlersNoParams.ContainsKey(key))
                _eventHandlersNoParams[key] = new List<Action>();
            _eventHandlersNoParams[key].Add(action);
        }

        public void AddEventHandler(Events key, Action<object> action)
        {
            if (!_eventHandlers.ContainsKey(key))
                _eventHandlers[key] = new List<Action<object>>();
            _eventHandlers[key].Add(action);
        }

        public void FireEvent(Events key, object param = null)
        {
            if (!_eventHandlers.ContainsKey(key))
                return;
            _eventHandlers[key].ForEach(target => 
            {
                if (_syncronizeInvoke.InvokeRequired)
                    _syncronizeInvoke.BeginInvoke(new Action(() => target.Invoke(param)), null);
                else
                    target.Invoke(param);
            });
        }

        public void RemoveHandler(Events key, Action handler)
        {
            if (!_eventHandlersNoParams.ContainsKey(key))
                return;
            _eventHandlersNoParams[key].RemoveAll(f => f == handler);
        }

        public void RemoveHandler(Events key, Action<object> handler)
        {
            if (!_eventHandlers.ContainsKey(key))
                return;
            _eventHandlers[key].RemoveAll(f => f == handler);
        }

    }
}
