[ ! -d "./liquidctl" ] && git clone https://github.com/liquidctl/liquidctl.git
cd liquidctl
git pull
python -m PyInstaller -F liquidctl/cli.py
cp dist/cli.exe ../