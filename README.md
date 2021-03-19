# StreamingWidget
ENG ver [README_Eng](https://github.com/boomxch/StreamingWidget/blob/master/README_Eng.md)  
  
Splatoon2の配信者用情報表示ツールです  
ナワバリ、ガチマッチ、リーグマッチ、プライベートマッチのパワーや勝率、K/D、塗りポイントなどの情報を表示します
![sample](https://user-images.githubusercontent.com/6965987/97128982-1aa25b80-1781-11eb-91da-8d4135c96968.png)

## 注意事項
- Windows用
- `.NET Core 3.1 Runtime`のインストールが必須

## アプリの起動に必要なランタイム
当アプリケーションは以前まで自己完結型アプリケーション(約160MB)でしたが、アップデート機能の追加に伴い、フレームワーク依存型アプリケーション(約8MB)に変更しました。  
この影響で、アプリケーションの起動には`.NET Core 3.1 Runtime`のインストールが必須となっています。  
下記ダウンロードリンクにアクセスして"Windows"タブ（デフォルト選択）から"Download x86"(自分のアプリケーションが32bit用に作成してあるため、x86でないと動きません)を選びインストールを行ってください。  
[ダウンロードリンク](https://dotnet.microsoft.com/download/dotnet-core/current/runtime)

## アプリケーションファイルダウンロードリンク
[ダウンロードリンク](https://github.com/boomxch/StreamingWidget/raw/master/Splatoon2StreamingWidget.exe)

### もし起動が上手くいかない方はこちらの64bit版(自己完結型アプリケーション)もお試しください
[ダウンロードリンク](https://1drv.ms/u/s!Am_cMZT26Ppfgax4zbCiV47P_tWJvA) (100MB↑なのでone driveへのリンクです)

## 更新情報
- エラーを含む通信の削減

## 使い方

### ログインしよう!
1. テキストボックスにURLが貼ってあるのでそれをコピーしてブラウザなどで開きましょう
2. ログインするか既にログインされてたら`この人にする`というボタンが置いてある画面が出てきます
3. `この人にする`ボタンを右クリックして`リンクのアドレスをコピー`を押します
4. 最初のURLが貼ってあるアプリのテキストボックスを消して、コピーしたリンクを貼り付けます
5. `Update session`をクリック エラーが出なければ10秒後ぐらいにログインが完了して、配信者用画面が出てきます

### 使ってみよう!
- OBSでウィンドウキャプチャ→フィルタでカラーキー（黒）の類似度1とかで周りの枠が丸くなります
- OBSとかで大きさ調整すると良いです
- 自動更新をオンにするか手動でボタンを押すとデータが更新されます
- ゲーム画面において、Finishが表示されたらすぐにデータの更新が可能です
- 何かあったら"data/log.txt"を添付して教えてほしいです
- オシャレにカスタマイズしてくださっている方のNoteです! [リンク](https://note.com/splat/n/n04081c71ac49)