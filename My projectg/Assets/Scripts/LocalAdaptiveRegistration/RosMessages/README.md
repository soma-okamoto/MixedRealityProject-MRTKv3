# ID付きボトル補正結果

購読トピックは `/P_place_with_id`、ROS 1型は `place_estimation/BottleDistribution`。
定義は `Definitions/place_estimation/msg/BottleDistribution.msg` に保存している。
生成クラスは `Generated/PlaceEstimation/msg/BottleDistribution.cs`、名前空間は
`RosSharp.RosBridgeClient.MessageTypes.PlaceEstimation`。

## メッセージ生成

プロジェクトのルートから Windows PowerShell とインストール済み .NET SDK を使って実行する。
プロジェクト同梱の ROS# `MessageAutoGen.GenerateSingleMessage` を `isRos2=false` で呼び出す。
生成された `.cs` は手で編集せず、定義変更時に再生成する。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "Assets/Scripts/LocalAdaptiveRegistration/Tools/GenerateBottleDistribution.ps1"
```

## IDと適用対象

- 既存の `BottleIdentity.Id` を `BottleAreaChecker.info.id` 経由で取得する。
- `P_currentAndStringPub.currentId` を辞書のキーと `/P_current.data[0]` に使う。
- `/P_current` は引き続き `Float32MultiArray [bottle_id, x, y, z]`。
- `ArmForBottle` はボトルとその ObjectList をIDで登録する。未初期化IDは拒否する。
- 同じID・同じボトルは再登録できる。同じID・別ボトルはエラーとして拒否する。
- 受信コールバックは `bottle_id` と位置・frame_id をコピーしてキューへ渡す。
- `Update` で受信IDから対象を検索する。未登録ID、破棄済みボトル／ObjectListはログを出して無視する。
- 登録は1回の受信後も保持される。`CancelPendingResult` は登録と受信キューを全消去する。
- `header.seq` は対象選択に使用しない。request_id／シーケンスIDは追加していない。

既存の座標変換 `(-ROS.x, -ROS.z, ROS.y)` と `ObjectList.TransformPoint` を維持し、
ボトルの位置だけを更新する。mainシーンの購読先も変更済み。
旧 `/P_place` の購読は行わず、以前の設定が残ったコンポーネントも起動時に新トピックへ移行する。

## 検証

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File "Assets/Scripts/LocalAdaptiveRegistration/Tools/VerifyBottleSubscriber.ps1"
```

.NET 10の検証プロジェクトが、実際の購読スクリプトと生成メッセージをコンパイルして実行する。
Unity APIと購読開始処理のみテスト用の代替実装を使い、Unity APIの呼び出しスレッドを検査する。
代替実装は `STANDALONE_BOTTLE_TEST` 条件付きであり、Unityの通常コンパイルでは無効。
ビルド出力は一時ディレクトリに作成する。

確認項目は、メッセージ形式、購読先移行、ID 7/12の対象選択、7→12→7、
未登録／-1、重複ID、破棄済み対象、既存変換と回転維持、400件の並行受信、
受信データのコピーと登録解除。Unity／ROS実機の通信・Play Modeは別途確認が必要。

`P_metaPositionDebugSubscriber`についても、12要素ごとのXYZ抽出、複数マーカー、
原点の回転・スケール、マーカー再利用と非表示、不正値、受信スナップショットを検証する。
現在は既存10項目と表示用4項目の計14項目。
