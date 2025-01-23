using UnityEngine;

public abstract class Trait : ScriptableObject
{
    [Header("Trait Info")]
    public string traitName;
    [TextArea]
    public string description;
    public Sprite icon;

    // Apply the trait's effect to the player
    public abstract void Apply(PlayerManagement player);
}
