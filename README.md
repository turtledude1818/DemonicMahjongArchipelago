# Demonic Mahjong Archipelago
An [Archipelago](https://github.com/ArchipelagoMW/Archipelago) implementation for the game [Demonic Mahjong](https://store.steampowered.com/app/3444020/Demonic_Mahjong/).

## Installation
Install the latest [BepInEx](https://github.com/bepinex/bepinex) bleeding edge IL2CPP build in the game's base Steam directory and place the folder in BepInEx's plugin folder. <br>
Install the apworld file through the Archipelago launcher.

## Notes
- DLC content is not yet implemeneted: Disable the DLC either on the main menu or in Steam.
- Reset or create a new save file before connecting to Archipelago.
- If restarting a multiworld with the same seed, delete the json with that seed's name in the game's save location. On Windows by default this is located in 
C:/Users/\{user\}/AppData/LocalLow/Boxed Lightning Games/Demonic Mahjong/Save <br>
Check the Bepinex console or log for a message starting with \"Save path:\" otherwise.
- There are a lot of locations that are available early in logic but are unlikely to happen without specifically pursuing them. Feel free to use hints/spoilers if another game is stuck.
- Do not use merit or essence to unlock locked items.

## Known Issues
- Newly received characters may not show properly in the Characters menu until re-entering the menu a second time.
- Stage clears will be checked even when losing to the final boss of that stage.
- The game may crash when entering incorrect connection information or disconnecting from the server.
- The client may not connect to Archipelago again after disconnecting; restart the game if needed.
- The Energy meter will not visually update when receiving an energy filler item; hover over it to see the value.