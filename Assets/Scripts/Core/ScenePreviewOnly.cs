using UnityEngine;

namespace Game.Core
{
    /// Editor-only texture-preview object: destroys itself on entering play mode (Awake),
    /// to avoid duplicating the content the runtime generates from code.
    /// Purpose: lets scenes that build their content at runtime (Title / Training) still show the real textures in the Scene view.
    public class ScenePreviewOnly : MonoBehaviour
    {
        private void Awake()
        {
            if (Application.isPlaying) Destroy(gameObject);
        }
    }
}
