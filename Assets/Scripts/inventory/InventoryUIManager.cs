using UnityEngine;
using TMPro;

public class InventoryUIManager : MonoBehaviour
{


    [SerializeField] private TMP_Text inventoryUIText;
    [SerializeField] private Inventory inventory;
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    // Update is called once per frame
    void Update()
    {
        UpdateInventoryUI(inventory); //probably wont affect performance too much since inventory is not expected to change every frame, but if it does, consider using an event to trigger the update instead of doing it every frame.
    }

    private void UpdateInventoryUI(Inventory inventory)
    {
        if (inventoryUIText == null) return;

        string text = "Inventory:\n";
        foreach (var item in inventory.GetItems())
        {
            text += $"- {item.name}\n";
        }
        inventoryUIText.text = text;
    }
}
