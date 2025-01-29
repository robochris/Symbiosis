using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    public string itemName;
    public string description;
    public Sprite icon;

    public abstract void Apply(PlayerManagement player);
    public virtual void Remove(PlayerManagement player) { }
}
