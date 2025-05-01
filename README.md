# WOLAP
A client mod for [West of Loathing](https://store.steampowered.com/app/597220/West_of_Loathing/), integrating it with the [Archipelago multiworld multi-game randomizer system](https://archipelago.gg/).

## WORK IN PROGRESS
This mod is very much incomplete, but it *should* be playable. Bug reports and all kinds of feedback are welcome, and should be directed to the West of Loathing thread in the `#future-game-design` channel on the [Archipelago Discord server](https://discord.gg/8Z65BR2).

## Installation
1. Locate your West of Loathing directory (on Steam, right-click on West of Loathing > Manage > Browse local files)
2. Download the latest stable release of [BepInEx](https://github.com/BepInEx/BepInEx/releases) (the x64 version)
3. Extract the contents of the downloaded .zip into the West of Loathing directory
4. Launch West of Loathing once.  Close it once it reaches the title screen, this is just to finish installing BepInEx.
5. Download the latest [WOLAP release](https://github.com/Lucasvdm/WOLAP/releases) and extract its contents
6. From the MonoMod folder, copy the MonoMod.Backports and MonoMod.ILHelpers .dll files into BepInEx/core
7. From the Newtonsoft folder, copy the Newtonsoft.Json.dll file into "West of Loathing_Data/Managed", overwriting the existing Newtonsoft.Json.dll
8. Copy the WOLAP folder (containing WOLAP.dll and Archipelago.MultiClient.Net.dll) into BepInEx/plugins

If you want to uninstall the mod, you can just delete the WOLAP folder in BepInEx/plugins.

The updated Newtonsoft.Json.dll file should have no negative impact on the game, but if you want to completely restore this file to the original version you can simply delete it and verify your game files on Steam (right-click West of Loathing > Properties > Installed Files > Verify integrity of game files).  Just know that the mod needs the updated file to work.

## What does this mod do?
The majority of the game's unique items and pickup locations have been randomized.  Currently, most non-unique loot and combat drops, unlimited shop items, and Foragin' plants are not included in the randomization.

The following extra Archipelago options have been implemented to configure the randomization:

#### Archipelago Options
- Enable Gun Manor DLC
  - Name in YAML file: `dlc_enabled`
  - This requires you to own the "Reckonin' at Gun Manor" DLC.  Disabling this while owning the DLC won't prevent you from accessing that content, it just won't include the DLC items and check locations in the randomization pool.
  - This is enabled by default
- Randomize Gun Manor Coach
  - Name in YAML file: `randomize_ghost_coach`
  - This randomizes the coach needed to access Gun Manor into the item pool
  - This is enabled by default, and has no effect unless the DLC is also enabled
- Randomize Goblintongue
  - Name in YAML file: `randomize_goblintongue`
  - This randomizes the ability to speak Goblintongue into the item pool
  - This is enabled by default.  If this is disabled, you will be able to speak Goblintongue from the start of the game.
- Start Inventory From Pool
  - Name in YAML file: `start_inventory_from_pool`
  - This allows you to specify items you will start with that then *won't* be included in the item pool -- as opposed to `start_inventory`, which lets you start with copies of items from the pool

In addition to the basic randomization, this mod makes various changes to the game's logic and mechanics to try to make it work better with Archipelago.  A full list of these changes can be found in the [docs](./docs/changelist.md).