# WOLAP
A client mod for [West of Loathing](https://store.steampowered.com/app/597220/West_of_Loathing/), integrating it with the [Archipelago multiworld multi-game randomizer system](https://archipelago.gg/).

## WORK IN PROGRESS
This mod is very much incomplete, but it *should* be playable. Bug reports and all kinds of feedback are welcome, and should be directed to the West of Loathing thread in the `#future-game-design` channel on the [Archipelago Discord server](https://discord.gg/8Z65BR2).

## Installation Instructions
There are now three different ways to install the WOLAP mod. You may choose to use Thunderstore Mod Manager, r2modman or manually install the mod yourself. Instructions for both installation methods are provided below.
**NOTE**: If you in the future are switching from Manual Installation to one of the Mod Manager installations please follow the uninstall instructions in Manual Installation

### Thunderstore Installation (Windows)
Thunderstore is a Mod Manager that is used to download and play modded versions of games without messing with the vanilla games files and without needing to know anything about file/folder manipulation. Use of Thunderstore requires Overwolf.

1. Download the Thunderstore application [Here](https://www.overwolf.com/app/thunderstore-thunderstore_mod_manager)
2. Open the Thunderstore application and enter "West of Loathing" into the search bar
3. Click on the option that appears when hovering over the game that says `Select Game`
4. Select or create a mod profile
5. On the left side of the screen, select `Get Mods`
6. Click on `WOLAP`, then download the most recent release and its dependencies
7. Once everything is downloaded, you should see `BepInEx` and `WOLAP` under `My Mods` on the left side of the screen. You may now launch the mod by selecting `(play) Modded` near the top-right of the application

The modded version of West of Loathing can be launched at any time using the `(play) Modded` button within Thunderstore Mod Manager while using the profile you selected/created priorly.

Installing WOLAP through Thunderstore Mod Manager should not interfere with the vanilla game files. You can continue launching the vanilla game normally through Steam, or by selecting `(play) Vanilla` next to the modded launch button within Thunderstore.

### r2modman Installation (Windows/Linux)
r2modman like Thunderstore is a Mod Manager that is used to download and play mods without messing with the vanilla game files. r2modman uses Thunderstore's website to gather Mods that are posted for specific games. Use of r2modman does **Not** require Overwolf and is more lightweight than Thunderstore

**Note**: This setup has **NOT** been tested for Linux, If you are willing to test it for Linux, feel free to ping @TylerJG92 in the West of Loathing Archipelago thread [Here](https://discord.com/channels/731205301247803413/1273856413327822950), post a copy of your BepInEx log file if it fails to work or any unexpected errors show up and I will look at it when I can. This should work in theory but if it dosent I would like to see why.

1. Download the r2modman application [Here](https://thunderstore.io/c/riskofrain2/p/ebkr/r2modman/) and follow the setup guide under `Installing` for the computer system you use.
2. Open the r2modman application and enter "West of Loathing" into the seach bar
3. Click on the option that appears when hovering over the game that says `Select Game`
4. Select or create a mod profile
5. On the left side of the screen, select `Online`
6. Click on `WOLAP`, then `Download` on the right then download the most recent release with dependencies
7. Once everything is downloaded, you should see `BepInEx` and `WOLAP` under `Installed` on the left side of the screen. You man now launch the mod by seleting `(play) Start modded` in the top left.

The modded version of West of Loathing can be launched at any time using the `(play) Start modded` button within r2modman while using the profile you selected or created priorly.

Installing WOLAP through r2modman should not interfere with the vanilla game files. You can continue launching the vanilla game normally through Steam or by selecting the dropdown next to `(play) Start modded` and then selecting the option `(play) Start vanilla` within r2modman.

### Manual Installation (Linux)

These instructions are for the native Linux version of West of Loathing running through Steam.

1. Locate your West of Loathing directory.
   - In Steam, right-click `West of Loathing` > `Properties` > `Installed Files` > `Browse`.

2. Download the latest stable Linux/macOS build of [BepInEx](https://github.com/BepInEx/BepInEx/releases).
   - Download the archive marked `nix`.
   - Unlike the Windows releases, the `nix` archive supports both 32-bit and 64-bit executables.

3. Extract the contents of the BepInEx archive directly into the West of Loathing directory.

4. Open a terminal in the West of Loathing directory and give the BepInEx launch script permission to run:

   ```bash
   chmod u+x run_bepinex.sh
   ```

5. In Steam, right-click `West of Loathing` > `Properties` and enter the following under `Launch Options`:

   ```text
   ./run_bepinex.sh %command%
   ```

6. Launch West of Loathing normally through Steam once. Close the game after reaching the title screen.
   - This allows BepInEx to finish its initial setup and create its required folders and files.

7. Download the latest [WOLAP release](https://github.com/TylerJG92/WOLAP/releases) and extract its contents.

8. From the `MonoMod` folder included with WOLAP, copy:
   - `MonoMod.Backports.dll`
   - `MonoMod.ILHelpers.dll`

   into:

   ```text
   BepInEx/core
   ```

9. From the `Patcher` folder included with WOLAP, copy:
   - `WOLAP.DependencyPatcher.dll`
   - `Newtonsoft.Json.dll`

   into:

   ```text
   BepInEx/patchers
   ```

10. Copy the `WOLAP` folder containing `WOLAP.dll` and `Archipelago.MultiClient.Net.dll` into:

    ```text
    BepInEx/plugins
    ```

11. Launch West of Loathing normally through Steam. Steam should now launch the game through BepInEx and load WOLAP.

#### Uninstalling the Linux Manual Installation

To uninstall WOLAP while keeping BepInEx installed:

- Delete the `WOLAP` folder from:

  ```text
  BepInEx/plugins
  ```

- Delete the following files from:

  ```text
  BepInEx/patchers
  ```

  - `WOLAP.DependencyPatcher.dll`
  - `Newtonsoft.Json.dll`

To completely remove the manual BepInEx installation:

- Delete the BepInEx files and folders that were added to the West of Loathing directory.
- Remove the following from the game's Steam `Launch Options`:

  ```text
  ./run_bepinex.sh %command%
  ```

- Verify the game's files through Steam if necessary.

### Manual Installation (Windows/macOS)

**NOTE** The new BepInEx\patchers option on step 7 has **NOT** been tested on Linux or macOS, If you would like to test those yourself, feel free to ping @TylerJG92 in the West of Loathing Archipelago thread [Here](https://discord.com/channels/731205301247803413/1273856413327822950), post a copy of your BepInEx log file if it fails to work or any unexpected errors show up and I will look at it when I can. This should work in theory but if it dosent I would like to see why.

1. Locate your West of Loathing directory (on Steam, right-click on West of Loathing > Manage > Browse local files)
2. Download the latest stable release of [BepInEx](https://github.com/BepInEx/BepInEx/releases) (the x64 version)
3. Extract the contents of the downloaded .zip into the West of Loathing directory
4. Launch West of Loathing once.  Close it once it reaches the title screen, this is just to finish installing BepInEx.
5. Download the latest [WOLAP release](https://github.com/TylerJG92/WOLAP/releases) and extract its contents
6. From the MonoMod folder, copy the MonoMod.Backports and MonoMod.ILHelpers .dll files into BepInEx\core
7. There are now 2 methods for this instruction, choose **1** of the following options:
  * From the Patcher folder, copy WOLAP.DependencyPatcher.dll and Newtonsoft.Json.dll into BepInEx\patchers
  * From the Patcher folder, copy the Newtonsoft.Json.dll file into "West of Loathing_Data\Managed", overwriting the existing Newtonsoft.Json.dll
    - Note: On Mac, there is no "West of Loathing_Data" folder. You instead need to right-click/Cmd-click on the West of Loathing app, then click "Show Package Contents" and go to Contents/Resources/Data to find the Newtonsoft.Json.dll.
8. Copy the WOLAP folder (containing WOLAP.dll and Archipelago.MultiClient.Net.dll) into BepInEx\plugins

- If you installed step 7 by replacing Newtonsoft.Json.dll in "West of Loathing_Data\Managed" and would like to uninstall: 
  - You can just delete the WOLAP folder in BepInEx\plugins.
    - The updated Newtonsoft.Json.dll file should have no negative impact on the game, but if you want to completely restore this file to the original version you can simply delete it and verify your game files on Steam (right-click West of Loathing > Properties > Installed Files > Verify integrity of game files).  Just know that the mod needs the updated file to work.

- If you installed step 7 by adding the WOLAP.DependencyPatcher.dll and Newtonsoft.Json.dll to BepInEx\patchers folder:
  - You can just delete the WOLAP folder in BepInEx\plugins and the two files, WOLAP.DependencyPatcher.dll and Newtonsoft.Json.dll, in BepInEx\patchers

- If you are uninstalling the Manual Installation to use one of the Mod Managers (Thunderstore/r2modman) Installations:
  - Delete the BepInEx folder within the West of Loathing Directory
  - Verify your game files on Steam (Right-click West of Loathing > Properties > Installed Files > Verify integrity of game files) *This will replace the Newtonsoft.Json.dll file if it was used to replace the original under "West of Loathing_Data\Managed"*

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
- Unbreakable Tools
  - Name in YAML file: `unbreakable_tools`
  - This removes the possibility of breaking or using up some important tools - shovel, pickaxe, El Vibrato headband - so you don't need to buy a replacement.
  - This is disabled by default.  Once you receive one of these tools from the item pool, you'll then be able to purchase that tool infinitely from Dirtwater Mercantile.
- Start Inventory From Pool
  - Name in YAML file: `start_inventory_from_pool`
  - This allows you to specify items you will start with that then *won't* be included in the item pool -- as opposed to `start_inventory`, which lets you start with copies of items from the pool

In addition to the basic randomization, this mod makes various changes to the game's logic and mechanics to try to make it work better with Archipelago.  A full list of these changes can be found in the [docs](./docs/changelist.md).

## AI Usage Disclosure
* WOLAP is **not** vibe-coded
* WOLAP does **not** contain AI art
This mod uses AI very minimally, Useage is as follows:
- Xylen:
  - In response to being asked on 8/4/26 if they used AI for the ap world: "Nope. In the interest of full, 100% honest disclosure, I used chatgpt exactly twice through development to try asking it about a couple of weird bugs that had me stuck. It basically just confirmed for me both times that the code I was looking at was fine so I went and manually found the bug elsewhere. None of the code (in the main games mod) is AI-generated" (https://discord.com/channels/731205301247803413/1273856413327822950/1534239231168479242)
- TylerJG92:
  - I do use ChatGPT as a development assistant to help me keep track of tasks, issues, long questlines/flags, and to explain code or help troubleshoot when I get stuck. I use strict working rules so it acts as a tutor and debugging assistant rather than writing the mod for me.
    - ChatGPT is in no way used for generating Gameplay and/or Mod code.
  - A small amount of AI-generated code has been used in internal packaging tools only. Those tools are not part of the code that runs in-game.