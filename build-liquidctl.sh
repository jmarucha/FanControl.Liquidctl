[ ! -d "./liquidctl" ] && git clone https://github.com/liquidctl/liquidctl.git
cd liquidctl
git pull
pyinstaller -F liquidctl/cli.py
cp dist/cli.exe ../