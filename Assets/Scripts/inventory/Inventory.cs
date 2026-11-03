using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SaveableEntity))]
public class Inventory : MonoBehaviour, ISaveable<Inventory.State>
{
    public class State
    {
        public List<string> keys = new();
    }

    readonly List<Key> keys = new();
    readonly List<string> unresolvedKeyIds = new();

    public void Add(Key key)
    {
        if (key && !keys.Contains(key)) keys.Add(key);
    }

    public bool Has(Key key) => key && keys.Contains(key);

    public bool Consume(Key key) => keys.Remove(key);

    public State CaptureState()
    {
        var state = new State();
        foreach (Key key in keys)
        {
            string id = key.SaveId;
            if (string.IsNullOrEmpty(id)) Debug.LogWarning($"[Save] key {key.name} has no SaveableEntity id, not saved", key);
            else state.keys.Add(id);
        }
        state.keys.AddRange(unresolvedKeyIds);
        return state;
    }

    public void RestoreState(State state)
    {
        keys.Clear();
        unresolvedKeyIds.Clear();
        foreach (string id in state.keys)
        {
            SaveableEntity entity = SaveSystem.FindEntity(id);
            Key key = entity ? entity.GetComponent<Key>() : null;
            if (key) keys.Add(key);
            else unresolvedKeyIds.Add(id);
        }
    }
}
