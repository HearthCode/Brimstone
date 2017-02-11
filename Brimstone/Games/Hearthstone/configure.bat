@echo off
set PROJECTDIR=%~1
if not exist "%PROJECTDIR%"Games\Hearthstone\Data\Cards.xml (
	if not exist "%PROJECTDIR%"Games\Hearthstone\Data (
		mkdir "%PROJECTDIR%"Games\Hearthstone\Data
	)
	git clone https://gitlab.com/HearthCode/DataExtractor
	pushd DataExtractor
	call build
	hs-data-extractor
	copy /y Cards.xml "%PROJECTDIR%"Games\Hearthstone\Data
	popd
	for /d %%p in ("DataExtractor") do rmdir "%%p" /s /q
)
