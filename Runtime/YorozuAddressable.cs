using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Yorozu
{
    public class YorozuAddressable : MonoBehaviour
    {
        private static YorozuAddressable _i;
        private static YorozuAddressable I
        {
            get
            {
                if (_i == null)
                {
                    var obj = new GameObject(nameof(YorozuAddressable), typeof(YorozuAddressable));
                    DontDestroyOnLoad(obj);
                    _i = obj.GetComponent<YorozuAddressable>();
                }
                
                return _i;
            }
        }

        private Dictionary<string, List<AsyncOperationHandle>> _handles = new Dictionary<string, List<AsyncOperationHandle>>();

        private void OnDestroy()
        {
            ReleaseAll();
            Object.Destroy(gameObject);
            _i = null;
        }

        public static async UniTask<bool> Contains(string address)
        {
            var opHandle = Addressables.LoadResourceLocationsAsync(address);
            await opHandle;

            if (opHandle.Status == AsyncOperationStatus.Succeeded &&
                opHandle.Result != null &&
                opHandle.Result.Count > 0)
            {
                return true;
            }

            return false;
        }

        public static async UniTask<T> LoadAndInstantiate<T>(string address, string key = "", Transform parent = null) where T : Component
        {
            var resource = await Load<GameObject>(address, key);
            var instance =Object.Instantiate(resource, parent);
            return instance.GetComponent<T>();
        }

        public static async UniTask<T[]> Loads<T>(string[] addresses, string key = "")
        {
            var tasks = new UniTask<T>[addresses.Length];
            for (var i = 0; i < addresses.Length; i++)
            {
                var address = addresses[i];
                tasks[i] = Load<T>(address, key);
            }

            return await tasks;
        }
        
        public static async UniTask<T> Load<T>(AssetReference reference, string key = "")
        {
            var handle = Addressables.LoadAssetAsync<T>(reference);
            await handle;
            if (!I._handles.ContainsKey(key))
            {
                I._handles.Add(key, new List<AsyncOperationHandle>());
            }
            I._handles[key].Add(handle);
            return handle.Result;
        }
        
        public static async UniTask<T> Load<T>(string address, string key = "")
        {
#if UNITY_EDITOR
            if (!await Contains(address))
            {
                Debug.LogError($"address: {address} not found.");
                return default;
            }
#endif
            var handle = Addressables.LoadAssetAsync<T>(address);
            await handle;
            if (!I._handles.ContainsKey(key))
            {
                I._handles.Add(key, new List<AsyncOperationHandle>());
            }
            I._handles[key].Add(handle);
            return handle.Result;
        }

        public static async UniTask<T> Load<T>(AsyncOperationHandle<T> handle, string key = "")
        {
            await handle;
            if (!I._handles.ContainsKey(key))
            {
                I._handles.Add(key, new List<AsyncOperationHandle>());
            }
            I._handles[key].Add(handle);
            return handle.Result;
        }

        public static void ReleaseAll()
        {
            var keys = I._handles.Keys.ToArray();
            foreach (var key in keys)
            {
                Release(key);
            }
            I._handles.Clear();
        }

        public static void Release(string key = "")
        {
            if (I._handles.TryGetValue(key, out var handles))
            {
                foreach (var handle in handles)
                {
                    Addressables.Release(handle);
                }
                handles.Clear();
                I._handles.Remove(key);
            }
        }
    }
}