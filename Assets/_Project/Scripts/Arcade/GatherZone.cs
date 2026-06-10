using UnityEngine;

namespace Nyangsta.Arcade
{
    public class GatherZone : InteractionZone
    {
        [SerializeField] private ArcadeStackItem itemPrefab;
        [SerializeField] private float gatherInterval = 0.5f;
        [SerializeField] private Transform spawnPoint;

        private float _timer;

        public void Configure(ArcadeStackItem prefab, float interval, Transform spawn)
        {
            itemPrefab = prefab;
            gatherInterval = Mathf.Max(0.05f, interval);
            spawnPoint = spawn;
        }

        protected override void OnAgentEnter(StackHolder agent)
        {
            _timer = gatherInterval;
        }

        protected override void OnAgentStay(StackHolder agent, float dt)
        {
            if (itemPrefab == null || !agent.CanAccept(itemPrefab.type)) return;

            _timer += dt;
            if (_timer < gatherInterval) return;
            _timer = 0f;

            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up * 0.8f;
            var item = Instantiate(itemPrefab, pos, Quaternion.identity);
            item.gameObject.SetActive(true);
            agent.Push(item);
        }
    }
}
