using Unity.Netcode;
using UnityEngine;

namespace Utility
{
    public abstract class StaticInstanceMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }
        protected virtual void Awake() => Instance = this as T;
    }
    
    public abstract class SingletonMono<T> : StaticInstanceMonoBehaviour<T> where T : MonoBehaviour
    {
        protected override void Awake()
        {
            if (Instance != null) 
                Destroy(gameObject);
            else
                base.Awake();
        }
    }

    public abstract class PersistentSingletonMonoBehaviour<T> : SingletonMono<T> where T : MonoBehaviour
    {
        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }
    }
    
    public abstract class StaticInstanceNetworkBehaviour<T> : NetworkBehaviour where T : NetworkBehaviour
    {
        public static T Instance { get; private set; }
        protected virtual void Awake() => Instance = this as T;
    }
    
    public abstract class SingletonNetworkBehaviour<T> : StaticInstanceNetworkBehaviour<T> where T : NetworkBehaviour
    {
        protected override void Awake()
        {
            if (Instance != null) 
                Destroy(gameObject);
            else
                base.Awake();
        }
    }

    public abstract class PersistentSingletonNetworkBehaviour<T> : SingletonNetworkBehaviour<T> where T : NetworkBehaviour
    {
        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }
    }
}