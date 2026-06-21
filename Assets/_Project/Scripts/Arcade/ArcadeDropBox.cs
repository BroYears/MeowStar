using UnityEngine;

namespace Nyangsta.Arcade
{
    /// <summary>
    /// DropBox / Storage box that facilitates raw resource logistics between split scenes.
    /// - Outdoor Scene: Acts as a SENDER. Takes resources from StackHolder and adds to ArcadeStorage.
    /// - Indoor Scene: Acts as a RECEIVER. Spawns resources from ArcadeStorage and pushes to StackHolder.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ArcadeDropBox : MonoBehaviour
    {
        [Header("Logistics Settings")]
        [SerializeField] private bool isReceiver = false; // true = Indoor receiver, false = Outdoor sender
        [SerializeField] private ArcadeItemType resourceType = ArcadeItemType.Fish;
        [SerializeField] private ArcadeStackItem itemPrefab;
        [SerializeField] private float interactInterval = 0.35f;

        private float _timer;

        private void Start()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerStay(Collider other)
        {
            _timer += Time.deltaTime;
            if (_timer < interactInterval) return;

            var holder = other.GetComponentInParent<StackHolder>();
            if (holder == null) return;

            _timer = 0f;

            if (isReceiver)
            {
                // Indoor: Spit resources from global storage to the character's stack
                if (holder.IsFull) return;
                
                int storedCount = ArcadeStorage.GetCount(resourceType);
                if (storedCount > 0)
                {
                    if (ArcadeStorage.RemoveResource(resourceType, 1))
                    {
                        var clone = Instantiate(itemPrefab);
                        clone.gameObject.SetActive(true);
                        holder.Push(clone);
                        Nyangsta.Audio.Sfx.Tap();
                    }
                }
            }
            else
            {
                // Outdoor: Drain resources from the character's stack to global storage
                if (holder.IsEmpty) return;

                // Look at the top item on stack
                var topItem = holder.Peek();
                if (topItem != null && topItem.type == resourceType)
                {
                    var popped = holder.Pop();
                    if (popped != null)
                    {
                        Destroy(popped.gameObject);
                        ArcadeStorage.AddResource(resourceType, 1);
                        Nyangsta.Audio.Sfx.Coin();
                    }
                }
            }
        }

        public void Configure(bool receiver, ArcadeItemType type, ArcadeStackItem prefab)
        {
            isReceiver = receiver;
            resourceType = type;
            itemPrefab = prefab;
        }
    }
}
