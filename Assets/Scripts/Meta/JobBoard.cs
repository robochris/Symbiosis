using UnityEngine;

namespace Shrunk
{
    /// <summary>The noticeboard in the lab: stand in front of it to read the three jobs pinned up and how far along each is.</summary>
    public class JobBoard : MonoBehaviour
    {
        public float readDistance = 1.9f;

        void OnEnable() => WorldPrompt.Add(Prompt, transform);
        void OnDisable() => WorldPrompt.Remove(Prompt);

        string Prompt(Vector3 playerPosition)
        {
            Vector3 to = playerPosition - transform.position;
            to.y = 0f;
            return to.magnitude <= readDistance ? Jobs.Describe() : null;
        }
    }
}
