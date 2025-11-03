# アーキテクチャパターン
各シーンごとに以下の構造で整理されています：
- **Manager/**: ビジネスロジックを担当
- **Scope/**: VContainerの依存性注入設定
- **Service/**: サービス層の実装
- **Starter/**: エントリーポイント
- **State/**: 状態管理
- **View/**: UI・ビューの実装

これらはTemplateSceneにフォルダが用意されているので、手動で作成する場合はこれをコピーしてください

### 依存方向（原則）
```
View  →  Service  →  State
  ↓          ↓
Starter    Manager     （Scope が全体を構成）
```
- 上位層 → 下位層の単方向依存のみを許可。
- NG 例: `State → Service` / `Service → View` の逆流依存。


# コーディング規約
- アクセス修飾子のprivateは省略すること
- privateなフィールドには_(アンダーバー)を付ける
- コンストラクタでのみ値が入るフィールドにはreadonlyを付ける
- ifの中身はパターンが推奨されている
例)
正: while (asyncLoad is { isDone: false})
誤: while (asyncLoad != null && !asyncLoad.isDone)  
