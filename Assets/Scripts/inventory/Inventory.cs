using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

[RequireComponent(typeof(SaveableEntity))]
public class Inventory : MonoBehaviour, ISaveable<Inventory.State>
{
    readonly List<Item> items = new();

    public IReadOnlyList<Item> GetItems() => items;
    readonly List<string> unresolvedItemIds = new();

    public event Action Changed;

    public void Add(Item item)
    {
        if (!item || items.Contains(item)) return;
        items.Add(item);
        Changed?.Invoke();
    }

    public bool Has(Item item) => item && items.Contains(item);

    public bool Has<T>() where T : Item => Get<T>();

    public T Get<T>() where T : Item
    {
        foreach (Item item in items)
            if (item is T match) return match;
        return null;
    }

    public bool Consume(Item item)
    {
        if (!items.Remove(item)) return false;
        Changed?.Invoke();
        return true;
    }


    // ---- ISaveable ----

    public class State
    {
        public List<string> items = new();
    }

    public State CaptureState()
    {
        var state = new State();
        foreach (Item item in items)
        {
            string id = item.SaveId;
            if (string.IsNullOrEmpty(id)) Debug.LogWarning($"[Save] item {item.name} has no SaveableEntity id, not saved", item);
            else state.items.Add(id);
        }
        state.items.AddRange(unresolvedItemIds);
        return state;
    }

    public void RestoreState(State state)
    {
        items.Clear();
        unresolvedItemIds.Clear();
        foreach (string id in state.items)
        {
            SaveableEntity entity = SaveSystem.FindEntity(id);
            Item item = entity ? entity.GetComponent<Item>() : null;
            if (item) items.Add(item);
            else unresolvedItemIds.Add(id);
        }
        Changed?.Invoke();
    }
    
}
