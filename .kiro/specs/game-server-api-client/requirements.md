# Requirements Document

## Introduction
本ドキュメントは、専用ゲームサーバとやり取りするためのAPIクライアントの要件を定義する。本APIクライアントは、Unity 6ベースの2Dゲームプロジェクトにおいて、VContainerによる依存性注入パターンに準拠し、全シーン共通のRootサービスとして動作する。UniTaskベースの非同期通信を提供し、ゲームサーバとのHTTP(S)通信を一元的に管理する。

なお、各APIエンドポイントに対応するUniTask型を返すメソッド群は、本APIクライアントとは別に定義される予定である。本specではHTTP通信基盤・認証・エラーハンドリングなどの共通基盤を対象とする。

## Requirements

### Requirement 1: HTTP通信基盤
**Objective:** As a 開発者, I want ゲームサーバへのHTTPリクエストを統一的なインターフェースで送信したい, so that 各APIメソッドから一貫した方法でサーバ通信を行える

#### Acceptance Criteria
1. The API Client shall GET・POST・PUT・DELETEの各HTTPメソッドによるリクエスト送信をサポートする
2. The API Client shall すべてのリクエストをUniTaskベースの非同期メソッドとして提供する
3. The API Client shall リクエストおよびレスポンスのボディをJSONでシリアライズ/デシリアライズする
4. The API Client shall リクエスト送信時に共通ヘッダー（Content-Typeなど）を自動付与する
5. When リクエストが送信される時, the API Client shall CancellationTokenによるキャンセルをサポートする

### Requirement 2: 認証管理
**Objective:** As a ゲームクライアント, I want 公開APIと認証APIの両方を利用したい, so that ユーザー登録・ログインから認証後の操作まで一貫してサーバ通信を行える

#### Acceptance Criteria
1. The API Client shall 認証不要な公開エンドポイント（ユーザー登録・ログインなど）へのリクエスト送信をサポートする
2. The API Client shall Bearer tokenを保持し、認証が必要なエンドポイントへのリクエストに自動的にAuthorizationヘッダーを付与する
3. When Bearer tokenが設定される時, the API Client shall 以降の認証付きリクエストにそのトークンを含める
4. When Bearer tokenがクリアされる時, the API Client shall 以降のリクエストからAuthorizationヘッダーを除外する

### Requirement 3: エラー分類と結果返却
**Objective:** As a 開発者, I want サーバ通信の結果を構造化された型で受け取りたい, so that 呼び出し元やエラーハンドリングサービスが適切に処理を分岐できる

#### Acceptance Criteria
1. The API Client shall リクエスト結果を成功またはエラーを表す構造化された型で返す
2. The API Client shall エラーを種別ごとに分類する（ネットワークエラー、タイムアウト、認証エラー、クライアントエラー4xx、サーバエラー5xx、パースエラー）
3. If HTTPステータスコードがエラー（4xx/5xx）を示す場合, the API Client shall ステータスコードとレスポンスボディを含むエラー情報を返す
4. The API Client shall エラーがリトライ可能かどうかを判定し、エラー情報に含める（ネットワークエラー・タイムアウト・5xxはリトライ可能、4xxはリトライ不可）
5. The API Client shall すべてのエラーをDebug.LogErrorでクラスコンテキスト付きでログ出力する
6. The API Client shall ダイアログ表示やシーン遷移などのUI操作を行わない

### Requirement 4: エラーハンドリングサービス
**Objective:** As a 開発者, I want API通信エラーに対する共通のUI処理とリトライ制御を一元化したい, so that 各呼び出し元がエラーハンドリングやリトライループを重複実装せずに済む

#### Acceptance Criteria
1. The Error Handling Service shall API呼び出しをデリゲートとして受け取り、実行・エラー処理・リトライを内包するメソッドを提供する
2. When デリゲートの実行が成功した時, the Error Handling Service shall 成功結果を呼び出し元に返す
3. If エラーがリトライ不可（4xx等）の場合, the Error Handling Service shall エラーメッセージダイアログを表示した後、タイトル画面に遷移する
4. If エラーがリトライ可能（ネットワークエラー・タイムアウト・5xx）の場合, the Error Handling Service shall 「リトライ」または「タイトルに戻る」の選択肢ダイアログを表示する
5. When ユーザーが「リトライ」を選択した時, the Error Handling Service shall デリゲートを再実行する
6. When ユーザーが「タイトルに戻る」を選択した時, the Error Handling Service shall タイトル画面に遷移する
7. The Error Handling Service shall VContainerを通じてRootScopeのSingletonサービスとして注入可能にする

### Requirement 5: 設定管理
**Objective:** As a 開発者, I want APIクライアントの接続設定を一元管理したい, so that 環境に応じた柔軟な設定変更ができる

#### Acceptance Criteria
1. The API Client shall ベースURLを設定可能にする
2. The API Client shall リクエストタイムアウト時間を設定可能にする

### Requirement 6: APIログ出力
**Objective:** As a 開発者, I want APIの通信内容をログで確認したい, so that 開発・デバッグ時にリクエストとレスポンスの詳細を追跡できる

#### Acceptance Criteria
1. The API Client shall リクエスト送信時にエンドポイントとリクエストボディをDebug.Logで出力する
2. The API Client shall レスポンス受信時にエンドポイント、レスポンスボディ、ステータスコード、レスポンスタイムをDebug.Logで出力する
3. The API Client shall コンパイルディレクティブにより本番ビルドではログ出力を無効化する（`PRODUCTION`が未定義の場合のみログ出力する）
4. While `PRODUCTION`が定義されている時, the API Client shall APIログ出力を一切行わない

### Requirement 7: DI統合
**Objective:** As a 開発者, I want APIクライアントおよびエラーハンドリングサービスをVContainerのDIパターンに統合したい, so that 既存のアーキテクチャと一貫した方法で利用できる

#### Acceptance Criteria
1. The API Client shall インターフェースを通じた抽象化を提供する
2. The API Client shall RootScopeにてSingletonとして登録可能にする
3. The Error Handling Service shall インターフェースを通じた抽象化を提供する
4. The Error Handling Service shall RootScopeにてSingletonとして登録可能にする
5. The API Client shall プロジェクトの依存方向ルール（View → Service → State）に従う
