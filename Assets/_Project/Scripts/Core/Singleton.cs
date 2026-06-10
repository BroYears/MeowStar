using UnityEngine;

namespace Nyangsta.Core
{
    /// <summary>Generic MonoBehaviour singleton base. Persists across scenes.</summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = (T)this;
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
            OnAwake();
        }

        /// <summary>Override instead of Awake() in subclasses.</summary>
        protected virtual void OnAwake() { }
    }
}
