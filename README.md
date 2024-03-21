# FanControl.Liquidctl

This is a simple plugin that uses [liquidctl](https://github.com/liquidctl/liquidctl) to provide sensor data and pump
control to variety of AIOs. So far it is tested with NZXT Kraken X63, but in principle shall work
with [supported devices](https://github.com/liquidctl/liquidctl#supported-devices)

## Installation

Grab a release and unpack it to `Plugins` directory of your FanControl instalation. It contains Windows bundle of
liquidctl.

## Setting up the developer environment

The project, after being imported to Visual Studio needs to have it reference to `FanControl.Plugins.dll`
and `Newtonsoft.Json.dll` from FanControl directory. You also need to create the executable of liquidctl, which can be
automatized with script `build-liquidctl.sh`.

## Screenshots

![Fluid temperature sensor](/docs/images/FluidTemp.png)
![Pump speed and control](/docs/images/PumpControl.png)

## License

MIT license, because it's superior.

```
Copyright (c) 2022 Jan K. Marucha

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
```

liquidctl, which is used by this plugin is provided
on [GPLv3](https://github.com/liquidctl/liquidctl/blob/main/LICENSE.txt).
