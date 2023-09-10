using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace Yorozu
{
    public static class AddressableUtility
    {
        public static string Combine(this string address, string fileName)
        {
            return $"{address}{fileName}";
        }
        
        public static string Combine(this string address, Enum fileName)
        {
            return $"{address}{fileName.ToString()}";
        }
        
        public static string Combine(this string address, int fileName)
        {
            return $"{address}{fileName}";
        }
        
        public static async UniTask<T> Load<T>(this AssetReference handle, string key = "")
        {
            return await YorozuAddressable.Load<T>(handle, key);
        }

        public static async UniTask<T> Load<T>(this AsyncOperationHandle<T> handle, string key = "")
        {
            return await YorozuAddressable.Load<T>(handle, key);
        }
        
        public static async UniTask<T> Load<T>(this string address, string key = "")
        {
            return await YorozuAddressable.Load<T>(address, key);
        }
        
        public static async UniTask<Sprite> LoadSprite(this string address, string key = "")
        {
            return await address.Load<Sprite>(key);
        }
        
        public static async UniTask<GameObject> LoadGameObject(this string address, string key = "")
        {
            return await address.Load<GameObject>(key);
        }
        
        public static async UniTask<Texture> LoadTexture(this string address, string key = "")
        {
            return await address.Load<Texture>(key);
        }
        
        public static async UniTask<Texture2D> LoadTexture2D(this string address, string key = "")
        {
            return await address.Load<Texture2D>(key);
        }
        
        public static async UniTask<AudioClip> LoadAudioClip(this string address, string key = "")
        {
            return await address.Load<AudioClip>(key);
        }

        public static async UniTask Load(this Image self, string address, string key = "")
        {
            self.enabled = false;
            self.sprite = null;
            var sprite = await address.LoadSprite();
            self.sprite = sprite;
            self.enabled = true;
        }
        
        public static async UniTask Load(this Image self, AssetReference assetReference, string key = "")
        {
            self.enabled = false;
            var sprite = await assetReference.Load<Sprite>();
            self.sprite = sprite;
            self.enabled = true;
        }
    }
}