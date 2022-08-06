# StreamingWidget
fork from https://github.com/boomxch/StreamingWidget

## Update version manually
When the Nintendo Online App updated, add the `version` field in `data/config.xml` correspondingly.
```xml
<?xml version="1.0" encoding="utf-8"?>
<UserData xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <user_name>AAAAAAAA</user_name>
  <session_token>BBBBBBBB</session_token>
  <iksm_session>CCCCCCCC</iksm_session>
  <principal_id>DDDDDDDD</principal_id>
  <verision>2.2.0</verision>
</UserData>
```

**TODO**
- ~~Fix layout of `Splatoon2StreamingWidget\StreamingWindow.xaml`~~
- ~~Auto update~~
