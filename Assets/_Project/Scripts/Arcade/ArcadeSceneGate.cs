using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// Trigger zone that transitions the player between the Outdoor Forest scene
    /// and the Indoor Restaurant scene, preserving their inventory stack.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ArcadeSceneGate : MonoBehaviour
    {
        [Header("Transition Settings")]
        [SerializeField] private string targetSceneName = "ArcadeRestaurant";
        [SerializeField] private float cooldown = 1.5f;

        private float _cooldownTimer;

        private void Start()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        public void ConfigureGate(string targetScene, float gateCooldown = 1.5f)
        {
            targetSceneName = targetScene;
            cooldown = gateCooldown;
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_cooldownTimer > 0f) return;

            // Check if player triggered the gate
            var controller = other.GetComponent<ArcadePlayerController>();
            if (controller != null)
            {
                var stack = controller.GetComponent<StackHolder>();
                if (stack != null)
                {
                    // Preserve stack contents before changing scene
                    ArcadeStackPreserver.SaveStack(stack);
                }

                _cooldownTimer = cooldown;
                Debug.Log($"[ArcadeSceneGate] Transitioning player to {targetSceneName}");
                SceneManager.LoadScene(targetSceneName);
            }
        }
    }
}
