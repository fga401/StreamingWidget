# StreamingWidget
Splatoon2のStreamingWidget

## 注意事項
- Windows用
- ".NET Core 3.1 Runtime"のインストールが必須

## アプリの起動に必要なランタイム
当アプリケーションは以前まで自己完結型アプリケーション(約160MB)でしたが、アップデート機能の追加に伴い、フレームワーク依存型アプリケーション(約8MB)に変更しました。  
この影響で、アプリケーションの起動には".NET Core 3.1 Runtime"のインストールが必須となっています。  
下記ダウンロードリンクにアクセスして"Windows"タブ（デフォルト選択）から"Download x86"(自分のアプリケーションが32bit用に作成してあるため、x86でないと動きません)を選びインストールを行ってください。  
[ダウンロードリンク](https://dotnet.microsoft.com/download/dotnet-core/current/runtime)

## アプリケーションファイルダウンロードリンク
[ダウンロードリンク](https://github.com/boomxch/StreamingWidget/raw/master/Splatoon2StreamingWidget.exe)

## 更新情報
- レギュラーマッチへの対応
- リーグマッチへの対応
- プライベートマッチへの暫定対応
- ウデマエが変化したときに変わらない問題を修正
- 各項目の配置を変更
- XP、LPの変化値の色合いを変更

## 使い方

### ログインしよう!
1. テキストボックスにURLが貼ってあるのでそれをコピーしてブラウザなどで開きましょう  
2. ログインするか既にログインされてたら「この人にする」ってボタンが置いてある画面が出てくる  
3. 「この人にする」ボタンを右クリックして「リンクのアドレスをコピー」を押す  
4. 最初のURLが貼ってあるアプリのテキストボックスを消して、コピーしたリンクを貼り付け  
5. Update sessionをクリック エラーが出なければ10秒後ぐらいにログインが完了して、配信者用画面が出てくる

### 使ってみよう!
- OBSでウィンドウキャプチャ→フィルタでカラーキー（黒）の類似度1とかで周りの枠が丸くなる
- OBSとかで大きさ調整すると良い
- 自動更新をオンにするか手動でボタンを押すとデータが更新される
- Finishが表示されてすぐに更新可能です
- 何かあったら"data/log.txt"を添付して教えてほしい

## 他ツール
- [SnipeChecker](https://github.com/boomxch/StreamingWidget/blob/master/tools/SnipeChecker/About.md)