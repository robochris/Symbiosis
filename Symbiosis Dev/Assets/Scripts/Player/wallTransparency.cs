using System.Collections.Generic;
using UnityEngine;

public class WallTransparency : MonoBehaviour
{
    [Tooltip("The target (typically the player) that the camera needs to see.")]
    public Transform target;
    [Tooltip("Which layers count as walls.")]
    public LayerMask wallLayer;
    [Tooltip("Radius of the sphere cast.")]
    public float sphereCastRadius = 0.5f;
    [Tooltip("Alpha value to set for transparent walls.")]
    [Range(0, 1)]
    public float transparentAlpha = 0.3f;

    // Lists to keep track of modified renderers and their original materials.
    private List<Renderer> transparentRenderers = new List<Renderer>();
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();

    void Update()
    {
        if (target == null)
            return;

        // Determine direction and distance from camera to target.
        Vector3 direction = (target.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, target.position);

        // Use SphereCastAll to detect all walls between camera and target.
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, sphereCastRadius, direction, distance, wallLayer);

        // Store which renderers we hit this frame.
        List<Renderer> hitRenderers = new List<Renderer>();

        foreach (RaycastHit hit in hits)
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null)
            {
                hitRenderers.Add(rend);
                // If not already transparent, adjust its material.
                if (!transparentRenderers.Contains(rend))
                {
                    transparentRenderers.Add(rend);
                    // Save original materials.
                    originalMaterials[rend] = rend.materials;

                    // Create new materials with adjusted alpha.
                    Material[] newMats = new Material[rend.materials.Length];
                    for (int i = 0; i < newMats.Length; i++)
                    {
                        newMats[i] = new Material(rend.materials[i]);
                        Color c = newMats[i].color;
                        c.a = transparentAlpha;
                        newMats[i].color = c;
                        // Change the rendering mode to transparent.
                        newMats[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        newMats[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        newMats[i].SetInt("_ZWrite", 0);
                        newMats[i].DisableKeyword("_ALPHATEST_ON");
                        newMats[i].EnableKeyword("_ALPHABLEND_ON");
                        newMats[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        newMats[i].renderQueue = 3000;
                    }
                    rend.materials = newMats;
                }
            }
        }

        // Revert any renderers that are no longer obstructing the view.
        for (int i = transparentRenderers.Count - 1; i >= 0; i--)
        {
            Renderer rend = transparentRenderers[i];
            if (!hitRenderers.Contains(rend))
            {
                if (originalMaterials.ContainsKey(rend))
                {
                    rend.materials = originalMaterials[rend];
                    originalMaterials.Remove(rend);
                }
                transparentRenderers.RemoveAt(i);
            }
        }
    }
}
