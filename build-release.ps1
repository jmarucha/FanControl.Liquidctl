$compress = @{
  Path = ".\bin\Release\FanControl.Liquidctl.dll", ".\liquidctl.exe", ".\liquidctl-license.txt"
  DestinationPath = ".\FanControl.Liquidctl.zip"
}
Compress-Archive @compress