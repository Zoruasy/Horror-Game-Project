using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    readonly List<Key> keys = new();

    public void Add(Key key)
    {
        if (key && !keys.Contains(key)) keys.Add(key);
    }

    public bool Has(Key key) => key && keys.Contains(key);

    public bool Consume(Key key) => keys.Remove(key);
}
