using System;
namespace AVR.Debugger.Interfaces
{
    public enum Events
    {
        None,
        FileLoaded,
        Debug_Starting,
        Debug_Started,
        Debug_BeforeEnter,
        Debug_Enter,
        Debug_AfterEnter,
        Debug_Leave,
        Debug_Stopping,
        Debug_Stopped,
        Debug_UnknownData
    }

    public interface IEventService
    {
        void AddEventHandler(Events key, Action<object> action);
        void AddEventHandler(Events key, Action action);
        void RemoveHandler(Events key, Action<object> handler);
        void RemoveHandler(Events key, Action handler);
        void FireEvent(Events key, object param);
    }
}
