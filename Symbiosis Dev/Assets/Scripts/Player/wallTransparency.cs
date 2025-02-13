using System.Collections.Generic;
using UnityEngine;

public class WallTransparency : MonoBehaviour
{
    public static WallTransparency Instance { get; private set; }
    [Header("References")]
    [Tooltip("The target (usually the player) that the camera needs to see.")]
    public Transform target;

    [Tooltip("Which layers count as walls.")]
    public LayerMask wallLayer;

    [Header("Transparency Settings")]
    [Tooltip("Radius of the sphere cast.")]
    public float sphereCastRadius = 0.5f;
    [Tooltip("How quickly walls fade in/out (seconds to reach target alpha). Set to 0 for instant.")]
    public float fadeSpeed = 2f;
    [Range(0, 1)]
    [Tooltip("Alpha value to set for transparent walls.")]
    public float transparentAlpha = 0.3f;

    /// <summary>
    /// Stores info about each renderer we fade.
    /// We keep track of current alpha, target alpha, and original materials.
    /// </summary>
    private class FadedRenderer
    {
        public Renderer renderer;
        public Material[] originalMaterials;   // The materials as they were before going transparent
        public Material[] fadeMaterials;       // Cloned materials for fading
        public float currentAlpha;
        public float targetAlpha;
    }

    

    // Keep a list of all renderers we’re currently handling
    private Dictionary<Renderer, FadedRenderer> fadedRenderers = new Dictionary<Renderer, FadedRenderer>();
    void Awake()
    {
        // Setup the singleton instance.
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!target) return;

        // 1) Cast a sphere from camera to target to find obstructing walls
        Vector3 direction = (target.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, target.position);

        RaycastHit[] hits = Physics.SphereCastAll(
            origin: transform.position,
            radius: sphereCastRadius,
            direction: direction,
            maxDistance: distance,
            layerMask: wallLayer
        );

        // Store which renderers are hit this frame
        HashSet<Renderer> hitThisFrame = new HashSet<Renderer>();

        foreach (RaycastHit hit in hits)
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null)
            {
                hitThisFrame.Add(rend);
                // If this renderer isn't in our dictionary, start fading it out
                if (!fadedRenderers.ContainsKey(rend))
                {
                    AddRenderer(rend);
                }

                // Update target alpha
                fadedRenderers[rend].targetAlpha = transparentAlpha;
            }
        }

        // 2) For each renderer we have, if it wasn't hit, fade it back to 1.0 alpha
        List<Renderer> toRemove = new List<Renderer>();
        foreach (var kvp in fadedRenderers)
        {
            Renderer rend = kvp.Key;
            FadedRenderer f = kvp.Value;

            // If not hit, move target alpha back to 1
            if (!hitThisFrame.Contains(rend))
            {
                f.targetAlpha = 1f;
            }
        }

        // 3) Update alpha for each renderer and remove it if fully opaque & no longer hit
        foreach (var kvp in fadedRenderers)
        {
            Renderer rend = kvp.Key;
            FadedRenderer f = kvp.Value;
            // Smoothly move towards the target alpha
            if (fadeSpeed > 0)
            {
                f.currentAlpha = Mathf.MoveTowards(
                    f.currentAlpha,
                    f.targetAlpha,
                    Time.deltaTime * fadeSpeed
                );
            }
            else
            {
                // Instant fade if fadeSpeed = 0
                f.currentAlpha = f.targetAlpha;
            }

            // Apply the updated alpha to its fade materials
            for (int i = 0; i < f.fadeMaterials.Length; i++)
            {
                Material mat = f.fadeMaterials[i];
                Color c = mat.color;
                c.a = f.currentAlpha;
                mat.color = c;
            }

            // If fully opaque and we want it back to original, restore
            if (Mathf.Approximately(f.currentAlpha, 1f) && Mathf.Approximately(f.targetAlpha, 1f))
            {
                toRemove.Add(rend);
            }
        }

        // 4) Restore & remove from dictionary
        foreach (Renderer r in toRemove)
        {
            RestoreRenderer(r);
        }
    }

    private void AddRenderer(Renderer rend)
    {
        // Create new FadedRenderer object
        FadedRenderer f = new FadedRenderer
        {
            renderer = rend,
            originalMaterials = rend.materials, // store the original array
            fadeMaterials = new Material[rend.materials.Length],
            currentAlpha = 1f,     // start as fully opaque
            targetAlpha = transparentAlpha
        };

        // Clone the materials so we don’t permanently change the original ones
        for (int i = 0; i < f.fadeMaterials.Length; i++)
        {
            Material clone = new Material(rend.materials[i]);
            SetupMaterialForTransparency(clone);
            f.fadeMaterials[i] = clone;
        }

        // Assign the fadeMaterials to the renderer
        rend.materials = f.fadeMaterials;

        // Add to dictionary
        fadedRenderers.Add(rend, f);
    }

    private void RestoreRenderer(Renderer rend)
    {
        if (!fadedRenderers.ContainsKey(rend)) return;
        FadedRenderer f = fadedRenderers[rend];

        // Restore original materials
        rend.materials = f.originalMaterials;

        fadedRenderers.Remove(rend);
    }

    private void SetupMaterialForTransparency(Material mat)
    {
        // Turn on alpha blending
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        // Ensure we start with alpha=1 if fully opaque, or you can set a default
        Color c = mat.color;
        c.a = 1f;
        mat.color = c;
    }

    public void ClearOldRenderers()
    {
        fadedRenderers.Clear();
    }
}
