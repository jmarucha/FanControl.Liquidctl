$compress = @{
  Path = ".\bin\Release\FanControl.Liquidctl.dll", ".\liquidctl.exe"
  DestinationPath = ".\FanControl.Liquidctl.zip"
}
Compress-Archive @compress