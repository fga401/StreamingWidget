# StreamingWidget
这是开发给splatoon 2 主播的显示信息的软件
显示信息包括推定分，胜率，K/D比，涂地p数等等，对于涂地，单排，组排，私房都有效
![sample](https://user-images.githubusercontent.com/6965987/97128982-1aa25b80-1781-11eb-91da-8d4135c96968.png)

## 通知
- 仅适用 Windows
- 要求安装 `.NET 5.0 Runtime` 运行环境

## 关于要求安装运行环境来启动app
这个软件以前是独立的软件（大约160M），但是随着更新后功能的增加，现在已将转变成基于`.NET 5.0 Runtime` 环境的软件（大约8M）
因此，安装 `.NET 5.0 Runtime` 是必要的
点击下面的链接，选择`Windows` （默认） -> `Run desktop apps` -> `Donwload x86` 下载安装
注意，由于我的软件是32位的，所以你需要安装32位的运行环境
[下载链接](https://dotnet.microsoft.com/download/dotnet/current/runtime)

## app文件下载链接
[下载链接](https://github.com/boomxch/StreamingWidget/raw/master/Splatoon2StreamingWidget.exe)

### 如果你无法启动app，请尝试64位的独立应用版本
[下载链接](https://1drv.ms/u/s!Am_cMZT26PpfhNUj5nX0jMnwYmvZfA?e=8wy7ab) (这是一个onedrive链接，因为文件大于100MB)

## 更新信息
- 支持API的改变

## 使用方法

### 登录
1. 复制app文本框里的链接，在浏览器中打开
2. 如果你登录完成了或者以前登录过,，你会看到一个 `选择此人` 的按钮
3. 右键 `选择此人` ，选择 `复制链接`
4. 删除app里原本的链接，粘贴上一步中的链接
5. 点击 `Update session`. 如果没有错误，10秒内会完成登录，玩家的信息会弹出来。

### 使用
- 在OBS里面，选择窗口采集 -> 滤镜 -> 色值 -> 关键颜色类型 -> 自定义颜色 -> 选择颜色 -> 选择屏幕颜色，适当调整平滑
- 打开自动更新(`自動更新`) 或者手动点击按钮来更新
- 当屏幕上出现结束的时候就可以就可以更新数据
- 如果你有问题，请告诉我，附带上"data/log.txt"文件
- 这里有一个玩家进行了自定义成一个很酷的形式[链接](https://note.com/splat/n/n04081c71ac49)
