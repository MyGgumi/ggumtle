using UnityEngine;

[System.Serializable]
public class ChestItem
{
    public string itemName;
    public Sprite itemIcon;
    public int quantity = 1;
    public string description;

    public ChestItem(string name, Sprite icon, int qty = 1, string desc = "")
    {
        itemName = name;
        itemIcon = icon;
        quantity = qty;
        description = desc;
    }
}
