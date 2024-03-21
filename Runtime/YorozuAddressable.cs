using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Yorozu
{
    public class YorozuAddressable : MonoBehaviour
    {
        
#if UNITY_EDITOR
        private const string MENU_PATH = "Yorozu/Addressable/Enable Auto Add Address";

        [MenuItem(MENU_PATH)]
        private static void Menu() => EditorPrefs.SetBool(MENU_PATH, !EnableAddAddress);

        [MenuItem(MENU_PATH, true)]
        private static bool MenuValidate()
        {
            UnityEditor.Menu.SetChecked(MENU_PATH, EnableAddAddress);
            return true;
        }
        
        private static bool EnableAddAddress => EditorPrefs.GetBool(MENU_PATH, false);

        /// <summary>
        /// アドレスからパスへの変換に利用する
        /// </summary>
        public static Func<string, string> AddressToPathFunction;
#endif
        
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

        private Dictionary<string, Dictionary<string, AsyncOperationHandle>> _handles = new Dictionary<string, Dictionary<string, AsyncOperationHandle>>();

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

        /// <summary>
        /// キャッシュしているHandleがあればそこからロードする
        /// </summary>
        private static bool TryGetHandle(string key, string address, out AsyncOperationHandle handle)
        {
            if (!I._handles.TryGetValue(key, out var handles))
            {
                handle = default;
                return false;
            }

            if (handles.TryGetValue(address, out handle))
                return true;
            
            handle = default;
            return false;
        }
        
        /// <summary>
        /// キャッシュする
        /// </summary>
        private static void RegisterHandle(string key, string address, AsyncOperationHandle handle)
        {
            if (!I._handles.ContainsKey(key))
            {
                I._handles.Add(key, new Dictionary<string, AsyncOperationHandle>());
            }
            I._handles[key].Add(address, handle);
        }
        
        public static async UniTask<T> Load<T>(AssetReference reference, string key = "")
        {
            if (TryGetHandle(key, reference.AssetGUID, out var cacheHandle))
                return (T) cacheHandle.Result;

            var handle = Addressables.LoadAssetAsync<T>(reference);
            await handle;
            RegisterHandle(key, reference.AssetGUID, handle);
            return handle.Result;
        }
        
        public static async UniTask<T> Load<T>(string address, string key = "")
        {
            if (TryGetHandle(key, address, out var cacheHandle))
                return (T) cacheHandle.Result;
            
#if UNITY_EDITOR
            if (EnableAddAddress)
            {
                var checkHandle = Addressables.LoadResourceLocationsAsync(address);
                await checkHandle.Task;
                // ロード成功した
                if (checkHandle.Status == AsyncOperationStatus.Succeeded && checkHandle.Result != null
                                                                    && checkHandle.Result.Count > 0)
                {
                    RegisterHandle(key, address, checkHandle);
                    return (T)checkHandle.Result;
                }
                
                // 失敗したので登録
                RegisterAddress(address);
            }
#endif
            var handle = Addressables.LoadAssetAsync<T>(address);
            await handle;
            RegisterHandle(key, address, handle);
            return handle.Result; 
        }

        public static async UniTask<T> Load<T>(AsyncOperationHandle<T> handle, string key = "")
        {
            if (TryGetHandle(key, handle.ToString(), out var cacheHandle))
                return (T) cacheHandle.Result;
            await handle;
            RegisterHandle(key, handle.ToString(), handle);
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
        
#if UNITY_EDITOR
        private static readonly string LOCATION_ID = "Temp";
        
        /// <summary>
        /// カタログにアドレスが登録されていないのであれば登録する
        /// </summary>
        private static void RegisterAddress(string address)
        {
            if (AddressToPathFunction == null)
            {
                Debug.LogError("Not Found AddressToPathFunction");
                return;
            }
            
            var path = AddressToPathFunction?.Invoke(address);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("Not Found");
                return;
            }
            
            var resourceProviders = Addressables.ResourceManager.ResourceProviders;
            if (resourceProviders.All(v => v.GetType() != typeof(AssetDatabaseProvider)))
            {
                resourceProviders.Add(new AssetDatabaseProvider());
            }

            var resourceLocationMap = Addressables.ResourceLocators
                .Where(v => v.GetType() == typeof(ResourceLocationMap))
                .FirstOrDefault(v => v.LocatorId == LOCATION_ID);
            if (resourceLocationMap == null)
            {
                resourceLocationMap = new ResourceLocationMap(LOCATION_ID);
                Addressables.AddResourceLocator(resourceLocationMap);
            }
        
            var location = new ResourceLocationBase(
                address, 
                path, 
                typeof(AssetDatabaseProvider).FullName,
                AssetDatabase.GetMainAssetTypeAtPath(path)
            );
            
            ((ResourceLocationMap)resourceLocationMap).Add(address, location);
        }
#endif
    }
}