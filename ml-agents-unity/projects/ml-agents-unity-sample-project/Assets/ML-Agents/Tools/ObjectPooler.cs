using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tools
{
    /// <summary>
    /// ObjectPooler class that efficiently reuses prefab instances to save on Instantiation and Destruction time
    /// </summary>
    public class ObjectPooler : Singleton<ObjectPooler>
    {
        /// <summary>
        /// How many instances of a given prefab can be unloaded (i.e., removed from the pool and destroyed) in 1 frame?
        /// </summary>
        private const int MAX_PER_PREFAB_UNLOADS_PER_FRAME = 1;

        private readonly Dictionary<Object, List<Object>> pooledGameObjects = new Dictionary<Object, List<Object>>();
        private readonly Dictionary<int, Object> prefabByInstanceID = new Dictionary<int, Object>();
        private readonly Dictionary<Object, Coroutine> poolUnloadCoroutines = new Dictionary<Object, Coroutine>();

        private void OnDestroy()
        {
            ClearPool();
        }
        
        private static void InitializeGameObject<T>(T obj) where T : Object
        {
            GameObject gameObj = obj is Component component ? component.gameObject : (obj as GameObject);
            Object prefabObj = ((T)Instance.prefabByInstanceID[obj.GetInstanceID()]);
            GameObject prefabGameObj =
                prefabObj is Component prefabComp ? prefabComp.gameObject : (prefabObj as GameObject);
            gameObj.transform.localScale = prefabGameObj.transform.localScale;
            gameObj.gameObject.SetActive(true);
        }

        private static T InstantiatePrefab<T>(T prefab, System.Func<T> instantiationFunction) where T : Object
        {
            if (prefab == null || instantiationFunction == null)
            {
                Debug.LogError("Attempting to instantiate a null prefab!");
                return null;
            }
            if (Instance.pooledGameObjects.ContainsKey(prefab))
            {
                List<Object> pooledPrefabInstances = Instance.pooledGameObjects[prefab];

                while (pooledPrefabInstances.Count > 0 && pooledPrefabInstances[0] == null)
                {
                    pooledPrefabInstances.RemoveAt(0);
                }

                if (pooledPrefabInstances.Count > 0)
                {
                    T pooledPrefab = pooledPrefabInstances[0] as T;
                    pooledPrefabInstances.RemoveAt(0);
                    return pooledPrefab;
                }
                else
                {
                    return InstantiateNewPrefab(prefab, instantiationFunction);
                }
            }
            else
            {
                return InstantiateNewPrefab(prefab, instantiationFunction);
            }
        }

        private static T InstantiateNewPrefab<T>(T prefab, System.Func<T> instantiationFunction) where T : Object
        {
            GameObject gameObj = prefab is Component component ? component.gameObject : (prefab as GameObject);

            if (instantiationFunction == null)
            {
                return null;
            }

            T g = instantiationFunction();

            if (g != null)
            {
                Instance.prefabByInstanceID.Add(g.GetInstanceID(), prefab);
            }

            return g;
        }

        /// <summary>
        /// Instantiates a prefab.  If possible, utilizes a previously pooled object
        /// </summary>
        /// <param name="prefab">Prefab to instantiate</param>
        /// <returns>Returns the instantiated prefab</returns>
        public static T InstantiateGameObject<T>(T prefab) where T : Object
        {
            return InstantiateGameObject(prefab, Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        /// Instantiates a prefab.  If possible, utilizes a previously pooled object
        /// </summary>
        /// <param name="prefab">Prefab to instantiate</param>
        /// <param name="parent">Transform the instantiated object will be a child of</param>
        /// <returns>Returns the instantiated prefab</returns>
        public static T InstantiateGameObject<T>(T prefab, Transform parent) where T : Object
        {
            GameObject gameObj = prefab is Component component ? component.gameObject : (prefab as GameObject);

            bool instantiatingPrefab = gameObj.scene.rootCount == 0;
            T g = InstantiatePrefab(prefab, () =>
            {
#if UNITY_EDITOR
                if (instantiatingPrefab)
                {
                    return UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent) as T;
                }
#endif
                return Instantiate(prefab, parent);
            });
            
            GameObject instanceObject = g is Component instanceComp ? instanceComp.gameObject : (g as GameObject);

            if (instanceObject != null && instanceObject.transform.parent != parent)
            {
                instanceObject.transform.SetParent(parent);
            }
            InitializeGameObject(g);
            return g;
        }

        /// <summary>
        /// Instantiates a prefab.  If possible, utilizes a previously pooled object
        /// </summary>
        /// <param name="prefab">Prefab to instantiate</param>
        /// <param name="position">World position of the instantiated object</param>
        /// <param name="rotation">World rotation of the instantiated object</param>
        /// <returns>Returns the instantiated prefab</returns>
        public static T InstantiateGameObject<T>(T prefab, Vector3 position, Quaternion rotation) where T : Object
        {
            return InstantiateGameObject(prefab, position, rotation, null);
        }
        
        public static T InstantiateGameObject<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent) where T : Object
        {
            GameObject prefabGameObj = prefab is Component component ? component.gameObject : (prefab as GameObject);

            bool instantiatingPrefab = prefabGameObj.scene.rootCount == 0;
            T g = InstantiatePrefab(prefab, () =>
            {
#if UNITY_EDITOR
                if (instantiatingPrefab)
                {
                    return UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent) as T;
                }
#endif
                return Instantiate(prefab, position, rotation, parent);
            });

            GameObject instanceGameObj = g is Component comp ? comp.gameObject : (g as GameObject);

            if (instanceGameObj != null)
            {
                if (instanceGameObj.transform.parent != parent)
                {
                    instanceGameObj.transform.SetParent(parent);
                }

                instanceGameObj.transform.position = position;
                instanceGameObj.transform.rotation = rotation;
            }

            InitializeGameObject(g);
            return g;
        }
        
        /// <summary>
        /// Pools a game object
        /// </summary>
        /// <param name="obj">GameObject to pool</param>
        public static void PoolGameObject<T>(T obj) where T : Object
        {
            if (Instance == null)
            {
                return;
            }
            
            GameObject gameObj = obj is Component component ? component.gameObject : (obj as GameObject);
            
            int objectInstanceID = obj.GetInstanceID();

            if (!Instance.prefabByInstanceID.ContainsKey(objectInstanceID))
            {
                Destroy(gameObj);
                return;
            }

            T originalPrefab = Instance.prefabByInstanceID[objectInstanceID] as T;

            if (!Instance.pooledGameObjects.ContainsKey(originalPrefab))
            {
                Instance.pooledGameObjects.Add(originalPrefab, new List<Object>());
            }

            if (!Instance.pooledGameObjects[originalPrefab].Contains(obj))
            {
                Instance.pooledGameObjects[originalPrefab].Add(obj);
            }

            if (gameObj == null || !gameObj.activeSelf)
            {
                return;
            }

            gameObj.SetActive(false);
        }
        
        /// <summary>
        /// Destroys all pooled GameObjects
        /// </summary>
        public static void ClearPool()
        {
            if(Instance == null)
                return;
            
            foreach (Coroutine coroutine in Instance.poolUnloadCoroutines.Values)
            {
                if (coroutine != null)
                {
                    Instance.StopCoroutine(coroutine);
                }
            }
            
            foreach (List<Object> spawnedObjectsOfPrefab in Instance.pooledGameObjects.Values)
            {
                if (spawnedObjectsOfPrefab == null)
                {
                    continue;
                }

                foreach (Object spawnedObject in spawnedObjectsOfPrefab)
                {
                    if (spawnedObject != null)
                    {
                        Instance.prefabByInstanceID.Remove(spawnedObject.GetInstanceID());
                        Destroy(spawnedObject is Component component? component.gameObject : (spawnedObject as GameObject));
                    }
                }

                spawnedObjectsOfPrefab.Clear();
            }
        }

        /// <summary>
        /// Destroys all pooled GameObjects tied to a given prefab, and removes the prefab-key entry
        /// from the dictionary of pooled objects.
        /// </summary>
        private void StartUnloadingGameObjectsOfTypeFromPool<T>(T prefab) where T : Object
        {
            if (!poolUnloadCoroutines.ContainsKey(prefab))
            {
                poolUnloadCoroutines[prefab] = StartCoroutine(PrefabInstancesPoolUnload(prefab));
            }
        }

        private IEnumerator PrefabInstancesPoolUnload<T>(T prefab) where T : Object
        {
            List<Object> instances = pooledGameObjects[prefab];
            
            while (instances.Count > 0)
            {
                int numInstances = instances.Count;
                int toUnloadThisFrame = Mathf.Min(numInstances, MAX_PER_PREFAB_UNLOADS_PER_FRAME);

                for (int i = 0; i < toUnloadThisFrame; i++)
                {
                    int endOfList = numInstances - 1 - i; // Remove instances from end of list
                    
                    T instance = instances[endOfList] as T;
                    instances.RemoveAt(endOfList);

                    if (instance != null)
                    {
                        Instance.prefabByInstanceID.Remove(instance.GetInstanceID());
                        Destroy(instance is Component component? component.gameObject : (instance as GameObject));
                    }
                }

                yield return null;
            }

            // Remove the coroutine from the coroutines dictionary.
            poolUnloadCoroutines.Remove(prefab);
            
            // Note that even if the pooledGameObjects list is empty, it is never removed from
            // its containing dictionary once it has been added.
        }
    }
}
