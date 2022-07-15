using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class RuntimeCollection<T> : ScriptableObject
{
    public event Action<T> ItemAdded;
    public event Action<T> ItemRemoved;
    public List<T> Items = new List<T>();

    public void Add(T item)
    {
        if (Items.Contains(item)) return;
        
        Items.Add(item);
        ItemAdded?.Invoke(item);
    }

    public void Remove(T item)
    {
        if (!Items.Contains(item)) return;
        
        Items.Remove(item);
        ItemRemoved?.Invoke(item);
    }
}