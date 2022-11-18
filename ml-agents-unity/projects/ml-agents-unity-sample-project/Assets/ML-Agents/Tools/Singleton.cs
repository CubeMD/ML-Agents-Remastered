using UnityEngine;

namespace Tools
{
    /// <summary>
    /// Creates a singleton reference, but does not create new instance when called.
    /// </summary>
    /// <typeparam name="T">The type of the instance reference</typeparam>
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        private static T _instance = null;

        public static T Instance
        {
            get
            {
                if (_instance == null) _instance = FindObjectOfType(typeof(T)) as T;
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            _instance = Instance;
        }
    }
}