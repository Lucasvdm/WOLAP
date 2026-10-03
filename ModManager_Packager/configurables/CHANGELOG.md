# Changelog

# v0.3.1

All save files and AP Worlds Generated with v0.3.0 will be fine to upgrade to v0.3.1 without causing the game to break. No regeneration needed.

## Client Mod Changes

- Fixed a bug where the player can accidentally be locked out of the Norton dialog until they have all the crowns and the honey jellybean
  - Intended design and what it was fixed now to do is to re-unlock the dialog with Norton once the player gets ANY of the crowns or the Honey Jellybean.

--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

## v0.3.0

> **Important:** v0.3.0 contains generation-breaking AP World changes, including new locations, renamed locations, and changed item identities. A new v0.3.0 AP World and a newly generated seed are required to use the new world logic and content.

## AP World Changes

### New Checks and Items

- Added a new `Clown Campsite - Circus Information` check for the alternate free Circus Ticket route.
- Added `Hellstrom Ranch - Lucky Horseshoe`.
- Added `Year Supply of Dynamite Crafted - 52 Dynamite`.
- Added four El Vibrato vending machine checks to the Soupstock Lode chamber:
  - `El Vibrato Chamber (Soupstock Lode) - Vending Machine KROKUZ CHOTAZAK`
  - `El Vibrato Chamber (Soupstock Lode) - Vending Machine KROKUZ GACHAKUZ`
  - `El Vibrato Chamber (Soupstock Lode) - Vending Machine KROKUZ NOHONOKSTA`
  - `El Vibrato Chamber (Soupstock Lode) - Vending Machine KROKUZ TASTA STAZAK`
- Added `El Vibrato Food Cube` and `El Vibrato Rum x3` to the filler pool.
- Split the old generic `El Vibrato Cylinder` into three location-specific progression items:
  - `El Vibrato Cylinder (Lost Dutch Oven Mine)`
  - `El Vibrato Cylinder (Curious Flat Plain)`
  - `El Vibrato Cylinder (Curious False Mountain)`

### El Vibrato Logic

- Updated El Vibrato progression to use the appropriate location-specific cylinder instead of requiring three copies of one generic cylinder.
    - Curious Abandoned Well facility progression now uses the Lost Dutch Oven Mine cylinder.
    - Curious Flat Plain machinery/checks use the Curious Flat Plain cylinder.
    - Curious False Mountain Chronokey fabrication uses the Curious False Mountain cylinder.
    - El Vibrato Quest Completion now requires the relevant progression from all three cylinder routes.
- Updated `Curious Flat Plain - Explorer Skeleton` to require the El Vibrato Transponder.
- Separated the Soupstock Lode El Vibrato chamber into its own logical region.
- Physical access to the Soupstock Lode chamber no longer requires the Transponder, while the four vending-machine checks themselves do.
- Cleaned up Lost Dutch Oven Mine logic so locations that can make use of readily available vanilla Can of Oil are not unnecessarily locked behind Percussive Maintenance.
- Fixed logic for the Soupstock Lode chamber chest and related El Vibrato checks following testing.

### Ghostwood Logic

- Reworked Ghostwood location logic to follow the actual quest sequence instead of allowing later checks to become logical from owning quest items too early.
- Later Ghostwood locations now build from the logical reachability of earlier quest milestones.
- Updated the logical sequence for:
  - Ghost Cactus
  - Sharpened Pencil
  - Issued Permit
  - Issued ID
  - Office Supply Stapler
  - Stapled Report
  - IDDTF
  - Staple Remover
  - The Final Form
  - Permit Finally Processed
- `Ghostwood Salooooon - Whiskey Bottle` now also requires the player to have logically reached the Issued ID portion of the Ghostwood quest.

### Progression and Access Logic

- Updated Tony's Boots logic so it can be unlocked through normal Fort of Darkness progression or with the Mushroom Map.
- Changed the Mushroom Map to progression classification so it can properly support Tony's Boots logic.
- Added the Lucky Cap requirement to `Circus Kid - Lucky Cap Trade`.
- Added the missing Shovel requirement to `Desert House - Macready's Grave`.
- Further restricted Macready's Grave logic to require reaching the appropriate Gun Manor progression.
- Removed Can of Oil as a required AP progression item and reclassified it as filler, since oil is readily obtainable through vanilla gameplay.
- Updated affected mine logic to account for Can of Oil being obtainable outside the AP item pool.
- Removed the Silver-Toothed Skull as a required Jumbleneck Mine safe logic gate.
- Fixed several additional logic issues discovered during final generation and gameplay testing.

### Item Pool and Classification Changes

- Reworked a large number of item classifications from `useful` to `filler` where the items do not actually need to be protected by Archipelago logic.
    - This gives players significantly more flexibility when excluding locations without starving the filler pool.
- Reclassified `The Worst Gun` as filler.
- Reclassified the Mushroom Map as progression.
- Reclassified Can of Oil as filler.
- Changed `Human Ashes x2` to always place two bundles in the pool, guaranteeing four ashes rather than relying on extra filler copies. This allows the client to reliably detect when all four ashes have been consumed and Dave B has become impossible.

### Location Naming and Cleanup

- Restored the vanilla name `Kaye Ridge Mine` instead of `Kole Ridge Mine`.
- Renamed several El Vibrato locations to include their associated overworld entrance, making checks easier to identify in trackers and spoiler logs.
- Renamed `Deepest Delve Mine (Level 2) - Bracelet` to `Deepest Delve Mine (Level 3) - Bracelet`.
- Clarified several other location names, including Circus, Cowrruption, Bizarre Ruin, and El Vibrato checks.

## Client Changes

### Missed Check Recovery

- Expanded the missed-check recovery system for checks that can become permanently unavailable through normal gameplay.
- Moved the missed-check recovery shop to Lloyd, the bartender at The Jewel Saloon in Dirtwater.
- Missed checks sent to Lloyd now receive randomized storage prices from **100 to 1250 Meat**.
- Added dialogue explaining Lloyd's randomized storage fees.
- Missed-check data is reconstructed when loading a save so forwarded checks continue to work across game sessions.
- The Silver Turnip plating check uses its original **5000 Meat** cost when recovered instead of receiving a random recovery price.

### Shop Hinting

- Added Archipelago hinting for progression items sold in randomized shops.
- Shop hints are sent when relevant progression inventory becomes available rather than requiring the player to manually scout the location.
- Added hinting support for Lloyd's missed-check recovery inventory.
- Updated hint creation to use the Archipelago-supported `Unspecified` hint status.
- Fixed Tony's Boots not triggering its shop hints.
- Added visit-aware hinting for Wanderin' Sally:
  - First visit only considers Items 1–8.
  - Second visit only considers Items 9–10.
  - Third visit only considers Items 11–13.
  - Sixth visit considers Item 14.
- This prevents Sally from hinting items that have not entered her inventory yet.
- Added safeguards for shop items whose AP scout information has not finished loading yet. (This seems to only be in the missed shop and I have only seen it show in the log on already hinted items.. Please ping @TylerJG92 in the discord or leave an issue on the github if you encouter log warning or notice the hinting isnt working for other or all shops.)

### Ghostwood Overhaul

- Reworked the flow of Ghostwood's bureaucracy quests to prevent AP-delivered items from accidentally skipping quest steps.
- Added a dedicated Ghostwood logging-quest progression flag so the quest follows the intended order.
- Added additional handling for the Ghostwood whiskey quest to reduce permanent lockouts from a vanilla bug.
- Added a way to borrow a Ghost Pencil for the whiskey quest when necessary, complete with additional Ghostwood paperwork.
- Added safeguards so the borrowed pencil cannot incorrectly be reclaimed while it is still being used by the mayor.
- Ghostwood ID names are now selected during the tutorial-skip sequence before entering the main game.
- AP-delivered Ghostwood Visitor IDs and Temporary Visitor Permits received before those names are selected are held until setup is complete, then granted correctly.
- Cleaned up and retested the Ghostwood dialogue and progression flow.

### New Checks and El Vibrato Changes

- Added client support for the three location-specific El Vibrato Cylinder items.
- Updated each cylinder to grant a separate in-game El Vibrato fuse so the three progression routes remain distinct.
- Added the `Clown Campsite - Circus Information` check to the game scripts.
- Added the `Hellstrom Ranch - Lucky Horseshoe` check.
- Added the `Year Supply of Dynamite Crafted - 52 Dynamite` check.
- Fixed the dynamite crafting check so it remains available after progressing beyond its original main-quest trigger point.
- Added all four Soupstock Lode El Vibrato vending-machine checks.
- The vending machine now allows each AP check to be completed once and tracks the four options separately.
- Once all four AP vending checks have been completed, the machine returns to its normal post-check behavior.
- Added client item handling for `El Vibrato Food Cube` and `El Vibrato Rum x3`.
- Fixed the 9th progressive El Vibrato container failing to send its check.

### Missed-Check and Sequence-Break Fixes

- Added missed-check forwarding for both General Gob's Hat and General Gob's Pistol when General Gob leaves without using the persuasion routes that normally award them.
- Fixed missed-check forwarding for `Lazy-A Dude Ranch - Cowsbane Harvest` when the seeds are sold to Barnaby Bob.
- Added forwarding for `Stearns Ranch Cellar - Blood Altar` if the Goblet of Blood is thrown into the Jumbleneck Mine pit before talking with the doll to get the password to the alter check.
- Added forwarding for `Circus - Slide Whistle Reward` if the Lost and Found is emptied before the Slide Whistle reward can be obtained.
- Added forwarding for `Halloway's Hideaway - Half of Curly's Map` if Curly's treasure is obtained before Halloway's map-half check.
- Added forwarding for `Gun Manor Parlor - Sofa Cushions (Murdered Chili)` when the gunless chili is stomped on to murder it.
- Added forwarding for `Jeweler's Cabin - Spectacles` when the binoculars are given to the blind soldier at fort unnessicary.
    - The Spectacles check can also remain available at the jeweler after the spectacles themselves have already been used.
- Added forwarding for `The Daveyard Mausoleum - The Skeleton of Dave B. Defeated` if all four Human Ashes are consumed before Dave B can be fought.
- Added forwarding for `Desert House - Macready's Grave` if the Gun Manor lawyer ghost is killed before being sent to investigate the grave.
- Added the Alamo Rent-A-Mule map unlock when resolving the bowlegged soldier through alternate routes, while forwarding the now-missed mule check to allow access to Region H without the Comedy Flier.

### Main Quest and Honey Bean Fixes

- Fixed the Honeyed Jellybean becoming permanently unavailable after certain main-quest progression.
- Prevented the Honeyed Jellybean from being eaten when the player does not currently have the Ant-Eye Virus.
    - Fixed its no-virus interaction so the proper dialogue appears.
- Added a Norton workaround that allows the player to enter Frisco before they are ready to resolve Norton or deal with the Ant-Eye Virus.
    - Using this workaround leaves Norton and the train quest unresolved so the player can return later with the required progression while still allowing the player to get the checks for Dr. Morton as well as unlocking the Honeyed Jellybean for purchase.

### Other Gameplay Fixes

- Fixed the Silver Turnip plating price being bypassed through the missed-check system.
- Fixed `The Great Garbanzo's Hideout - Bean-Iron Deposit` missing its normal interaction dialogue.
- Fixed Saint Pope being fightable repeatedly.
- Fixed several Ghostwood dialogue/progression edge cases discovered during testing.
- Restored Can of Oil to Dirtwater Mercantile now that it is no longer treated as an AP progression requirement.
- Fixed several location-name mismatches between the client and AP World.
- Adjusted AP location scouting waits to reduce delayed item-result dialogue appearing after the player has already moved on, while reducing the noticeable freeze caused by longer waits.

### Archipelago / Client Infrastructure

- Updated `Archipelago.MultiClient.Net` from **6.6.1 to 6.7.1**.
- Updated the client version to **v0.3.0**.
- Improved save-load rebuilding for missed-check and shop-related state.
- Added additional safeguards around item scouting and shop hint processing.

-------------------------------------------------------------------------------------------------------------------------

## v0.2.4
- Updated Thunderstore to correct Game version
- Added Notation about AI Usage per Archipelago Community Rules
- Updated additional documentation to now include correct installation methods

-------------------------------------------------------------------------------------------------------------------------

## v0.2.3
- Added support for installing WOLAP through Thunderstore and Thunderstore-compatible mod managers.
- Added automatic handling of the required WOLAP dependencies so the game's original files no longer need to be manually replaced during installation.