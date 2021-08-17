# StreamingWidget
This is the information display tool for Splatoon2 streamers
Displays information such as power, win percentage, K/D, paint points, etc. for Turf War, Ranked Battle, League Battle, and Private Match
![sample](https://user-images.githubusercontent.com/6965987/97128982-1aa25b80-1781-11eb-91da-8d4135c96968.png)

## Notification
- For Windows
- It requires a install of `.NET 5.0 Runtime`

## About the runtime required to launch the app
This application used to be a self-contained application (about 160MB), but with the addition of the update feature, it has been changed to a framework-dependent application (about 8MB).    
Due to this effect, the installation of `.NET 5.0 Runtime` is required to launch the application.  
Go to the download link below and select `Donwload x86` in `Run desktop apps` from tab `Windows` (default selection) to install.  
Note that you will need to install x86 ver since my application was created for 32bit.  
[Download Link](https://dotnet.microsoft.com/download/dotnet/current/runtime)

## Application file download link
[Download Link](https://github.com/boomxch/StreamingWidget/raw/master/Splatoon2StreamingWidget.exe)

### If you have trouble starting the application, please try this 64bit version (self-contained application)
[Download Link](https://1drv.ms/u/s!Am_cMZT26Ppfgctv_ckv94_Ts9heeA) (This is a link to One Drive because it is over 100MB)

## Update Infomation
- Support for API changes

## Usage

### Let's login!
1. Copy the URL in the text box and open it in your browser
2. If you're logged in or already logged in, you'll see a screen with a button that says `Choose this person`
3. Right-click on the `Choose this person` button and press `Copy link address`
4. Delete the text box of the app with the first URL and paste the copied link
5. Click `Update session`. If there are no errors, the login will be completed in about 10 seconds and the streamer's screen will appear.

### Let's use it!
- In OBS, go to Window Capture -> Filter, and use the color key (black) with a similarity of 1 or so to make the surrounding frame round
- Turn on automatic update(`自動更新`) or press the button manually to update the data
- You can update the data as soon as Finish is displayed on the game screen
- If you have any problems, please let me know by attaching "data/log.txt"
- Here's a Note from someone who has customized it in a stylish way! [Link](https://note.com/splat/n/n04081c71ac49)