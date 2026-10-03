# ストア privacy 申告メモ（Data safety / App Privacy）

Google Play の **Data safety** と App Store Connect の **App Privacy** に入力する内容を、
このリポジトリの実装から確認できた事実にもとづいて整理したもの。
入力先はストアのコンソールなので、このファイルは申告の下書き兼エビデンスとして使う。

- 対象: `com.hemuichi.maruneko`（iOS / Android）
- 根拠を取った時点: 2026-10-03 / Unity 6000.3.12f1

---

## 1. アプリが扱うデータの実態

### アプリ自身

| 項目 | 実態 | 根拠 |
| --- | --- | --- |
| ネットワーク通信 | **なし**（`UnityWebRequest` / `HttpClient` の利用箇所なし） | `Assets/Scripts` 全体の grep |
| セーブデータ | **端末内のみ**（`PlayerPrefs` に JSON 保存。タイマー記録・設定・所持アイテム・ポイント） | `Root/Service/PlayerPrefsService.cs` |
| Addressables | ローカルカタログのみ（リモートロードパス未使用） | `AddressableAssetSettings.asset` |
| 解析 / クラッシュレポート / 課金 | **すべて無効** | `ProjectSettings/UnityConnectSettings.asset`（Analytics / CrashReporting / Purchasing すべて `m_Enabled: 0`） |
| アカウント機構 | なし（ログイン・ユーザー ID の概念がない） | — |

→ **アプリ自身はデータを一切送信していない。** 申告対象になるのは広告 SDK 経由の収集のみ。

### 広告 SDK

| SDK | バージョン |
| --- | --- |
| Unity LevelPlay (ironSource mediation) | `com.unity.services.levelplay` 9.4.1 / `mediation-sdk` 9.4.2 |
| Unity Ads アダプタ | `unityads-adapter` 5.7.0 / `unity-ads` 4.18.0 |
| Google Play 広告 ID | `play-services-ads-identifier` 18.1.0 |

ビルド済み AndroidManifest にマージされる権限（`Library/Bee/.../merged_manifests` で確認）:

- `com.google.android.gms.permission.AD_ID` ← **広告 ID を使う申告が必須になる根拠**
- `android.permission.ACCESS_ADSERVICES_ATTRIBUTION` / `ACCESS_ADSERVICES_TOPICS`（Privacy Sandbox）
- `android.permission.INTERNET` / `ACCESS_NETWORK_STATE` / `WAKE_LOCK` / `FOREGROUND_SERVICE` / `RECEIVE_BOOT_COMPLETED`

いずれもアプリ側の `AndroidManifest.xml` では宣言しておらず、SDK の AAR から自動マージされる。

---

## 2. Google Play — Data safety

### 入り口の設定

| 質問 | 回答 |
| --- | --- |
| アプリはユーザーデータを収集または共有しますか | **はい** |
| 転送中のデータは暗号化されますか | **はい**（SDK の通信は HTTPS） |
| ユーザーがデータの削除をリクエストする手段を提供していますか | **いいえ**（アカウントがないため。広告 ID のリセット手順はプライバシーポリシーに記載する） |
| 独立したセキュリティレビューを受けましたか | いいえ |

### データ種別

| カテゴリ | データ種別 | 収集 | 共有 | 必須/任意 | 目的 |
| --- | --- | --- | --- | --- | --- |
| デバイスまたはその他の ID | デバイスまたはその他の ID | ✓ | ✓ | **必須**（アプリ内にオプトアウト導線がないため） | 広告またはマーケティング |

上記 1 行が、このアプリで**確実に**申告が必要になる行。広告 ID（AD_ID / IDFA）を広告配信と計測に使い、
広告ネットワークへ渡している（= 共有）。

次の 2 件は LevelPlay / 各アドネットワークの収集内容に依存するため、**提出前に SDK 側の公開情報で確認する**:

| カテゴリ | データ種別 | 判断の目安 |
| --- | --- | --- |
| アプリのアクティビティ | アプリのインタラクション | 広告の表示・クリックイベントを計測に送る構成なら申告対象 |
| 位置情報 | おおよその位置情報 | IP から地域を推定してターゲティングする場合は申告対象（IP 単体の収集は対象外） |

> 確認先: Unity LevelPlay のドキュメントにある Data safety / privacy 向けの収集データ一覧。
> 媒介するネットワークを増やしたら、そのネットワーク分も申告を見直す。

### Data safety 以外に埋まっていない申告

| 場所 | 内容 |
| --- | --- |
| アプリのコンテンツ → 広告 | 「アプリに広告が含まれます」= **はい** |
| アプリのコンテンツ → 広告 ID | 「広告 ID を使用します」= **はい** / 用途: 広告・マーケティング |
| アプリのコンテンツ → 対象年齢 | **13 歳未満を対象にしない**こと。ファミリー向けに申告すると広告 ID の利用自体が制限される |
| ストア掲載情報 | **プライバシーポリシー URL（必須）** |

---

## 3. App Store — App Privacy

### トラッキング

| 質問 | 回答 |
| --- | --- |
| このアプリはトラッキングのためにデータを使用しますか | **はい** |

→ ATT（App Tracking Transparency）の許諾ダイアログが必須。実装状況は 4 章を参照。

### 収集データ（Data Types）

| カテゴリ | データ種別 | 用途 | ユーザーに紐付け | トラッキングに使用 |
| --- | --- | --- | --- | --- |
| Identifiers | Device ID（IDFA / IDFV） | サードパーティ広告 | いいえ | **はい** |
| Usage Data | Advertising Data | サードパーティ広告 | いいえ | **はい** |

- 「ユーザーに紐付け」= いいえ：アカウント機構がなく、広告 ID を個人と結び付けていない
- Diagnostics（クラッシュログ等）は **収集なし**（Unity Cloud Diagnostics 無効）
- App Store Connect でも**プライバシーポリシー URL は必須**

---

## 4. コード側の対応状況

| 項目 | 状態 | 場所 |
| --- | --- | --- |
| iOS: ATT 許諾を SDK 初期化前に取得 | 実装済み | `Root/Service/LevelPlayRewardedAdService.cs` の `RequestTrackingAuthorizationIfNeededAsync` |
| iOS: `NSUserTrackingUsageDescription`（ATT ダイアログの文言） | 実装済み（ビルド後に Info.plist へ注入） | `Assets/Editor/RewardedAdBuildPostProcessor.cs` |
| iOS: プライバシーマニフェスト（`PrivacyInfo.xcprivacy`） | 実装済み。`NSPrivacyTracking` と `NSPrivacyCollectedDataTypes` を注入。Unity が出力する `NSPrivacyAccessedAPITypes`（Required Reason API）はそのまま保持する | 同上 |
| Android: `AD_ID` 権限 | SDK の AAR から自動マージ（アプリ側の記述は不要） | `ProjectSettings/AndroidResolverDependencies.xml` |
| リリースビルドのログ抑止 | 実装済み（`Debug.Log` / `LogWarning` を停止。`PlayerPrefs` の中身が端末ログに出なくなる） | `Root/Scope/RootScope.cs` の `ConfigureLogging` |

### 未対応で提出をブロックするもの

- **プライバシーポリシー / 利用規約の本文と URL**。メニューのボタンは現在「未実装」ダイアログを出すだけ
  （`Menu/View/MenuDialog.cs`）。両ストアとも URL が必須なので、公開ページを用意してボタンから開くようにする。

### 申告内容を変えたら見直すもの

- `Assets/Editor/RewardedAdBuildPostProcessor.cs` の `NSPrivacyCollectedDataTypes`
  （App Store Connect の入力と食い違わせない）
- 同ファイルの `TrackingDomains` は**意図的に空**。アプリ自身がトラッキング目的で接続するドメインはなく、
  SDK は自前のプライバシーマニフェストでドメインを宣言するため。アプリから直接計測エンドポイントを叩き始めたら追記する。

---

## 5. 画面の向き（参考）

縦固定は Player Settings で設定済み（`defaultScreenOrientation: 0` = Portrait、縦以外の自動回転をすべて無効）。
Android では `android:screenOrientation="portrait"` として出力される。

残っている制約を 1 つだけ記録しておく:

- `androidResizeableActivity` は `true` のまま。マルチウィンドウ / 分割画面では向きの指定が無視される。
  また targetSdk 36（Android 16）では大画面端末で向きの制限自体が無視される。
  完全に縦だけにしたい場合は `resizeableActivity = false` にするが、マルチウィンドウ対応を落とす判断になるので未変更。
