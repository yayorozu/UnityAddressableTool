# UnityYorozuAddressable

UnityYorozuAddressable は、Unity の Addressable Assets システムを簡単に扱えるようにするためのラッパークラス群を提供するライブラリです。
本ライブラリを使用することで、Addressable アセットの読み込みや管理をシンプルな API で実装できます。

※ UniTaskが必要です

## 使い方
以下は、YorozuAddressable クラスを用いたアセットの読み込み例です。

### 1. Load を使ってアセットを非同期に読み込む
Load<T>(string address, string key = "") を利用して、アドレス指定でアセットを読み込みます。
※ key はキャッシュ管理用の識別子です。特定のグループで管理したい場合に指定します。デフォルトは空文字列です。

```csharp
using UnityEngine;
using Cysharp.Threading.Tasks;
using Yorozu; // YorozuAddressable の名前空間

public class LoadSample : MonoBehaviour
{
    // 読み込みに使用するアセットのアドレス
    [SerializeField] private string assetAddress = "MyPrefabAddress";
    // 複数のアセットをグループ管理する場合に利用するキー
    [SerializeField] private string loadKey = "MyLoadGroup";

    private GameObject _instance;

    private async void Start()
    {
        // 非同期に GameObject を読み込み
        var prefab = await YorozuAddressable.Load<GameObject>(assetAddress, loadKey);
        if (prefab != null)
        {
            // アセットのインスタンスを生成
            _instance = Instantiate(prefab);
            Debug.Log("アセットの読み込みと生成に成功しました");
        }
        else
        {
            Debug.LogError("アセットの読み込みに失敗しました: " + assetAddress);
        }
    }
}
```


### 2. Release を使って特定のグループのキャッシュを解放する
Release(string key = "") を呼び出すことで、指定した key に対応するキャッシュ済みのハンドルをすべて解放します。
※ 例えば、読み込んだアセット群が不要になったタイミングで呼び出してください。

```csharp
using UnityEngine;
using Yorozu;

public class ReleaseSample : MonoBehaviour
{
    // 先ほどの Load 時と同じキーを利用
    [SerializeField] private string loadKey = "MyLoadGroup";

    // 任意のタイミング（例えばシーン切り替え前など）で解放
    private void OnDisable()
    {
        YorozuAddressable.Release(loadKey);
        Debug.Log("LoadGroup のキャッシュを解放しました: " + loadKey);
    }
}
```

### 3. ReleaseAll を使って全てのキャッシュを解放する
ReleaseAll() を呼び出すと、登録されているすべてのキャッシュ済みハンドルを解放します。
※ アプリケーション終了時やシーン遷移時など、全アセットの解放を一括で行いたい場合に利用します。

```csharp
using UnityEngine;
using Yorozu;

public class ReleaseAllSample : MonoBehaviour
{
    // 例えば、シーンの OnDestroy 内で全て解放する場合
    private void OnDestroy()
    {
        YorozuAddressable.ReleaseAll();
        Debug.Log("全てのキャッシュを解放しました");
    }
}
```

## ライセンス
本プロジェクトは MIT License の下でライセンスされています。
詳細については、LICENSE ファイルをご覧ください。
