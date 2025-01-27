using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public interface IMessage
{

}

public static class EventManager
{
    private static readonly Dictionary<Type, Action<IMessage>> Events = new Dictionary<Type, Action<IMessage>>();

    private static readonly Dictionary<Delegate, Action<IMessage>> EventLookups = new Dictionary<Delegate, Action<IMessage>>();
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        Events.Clear();
        EventLookups.Clear();
    }

    public static void Subscribe<T>(Action<T> evt) where T : IMessage
    {
        if (!EventLookups.ContainsKey(evt))
        {
            Action<IMessage> newAction = (e) => evt((T)e);
            EventLookups[evt] = newAction;
            
            if (Events.ContainsKey(typeof(T)))
                Events[typeof(T)] += newAction;
            else
                Events[typeof(T)] = newAction;
        }
    }

    public static void Unsubscribe<T>(Action<T> evt) where T : IMessage
    {
        if (EventLookups.TryGetValue(evt, out var action))
        {
            if (Events.TryGetValue(typeof(T), out var tempAction))
            {
                tempAction -= action;
                if (tempAction == null)
                    Events.Remove(typeof(T));
                else
                    Events[typeof(T)] = tempAction;
            }

            EventLookups.Remove(evt);
        }
    }

    public static void TriggerEvent<T>(T message) where T : IMessage
    {
        if (Events.TryGetValue(typeof(T), out var action))
            action.Invoke(message);
    }

}
