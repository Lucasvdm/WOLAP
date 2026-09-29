# Changelog for v0.3.0

## Ghostwood Changes
* Fixed the flow of the quests within Ghostwood
    * Added a flag `gwloggingquestprogression` as another flag gate to make sure that each step of the ghostwood logging permit is done in the right order instead of items accidentally forcing flag progression elsewhere when it shouldnt.
    * player can be locked out of parts of the ghost whiskey quest, While this is intended in vanilla, I want to remove the problem incase the right conditions exist
        * Added a new dialogue that basically allows the player to borrow a ghost pencil at the cost of filling out paperwork to borrow it and additional paperwork for each item it will be used for
            * Added a flag gate that if the mayor still has the players ghost pencil and they had chosen to borrow the pencil for the whiskey quest that when they go back to the borrowee they will state that they cannot allow you to take the pencil from their store as it is hard to come by.

## Item Routing fixes and features
* Added hinting behavior to all shops to hint `Progressive` items to the ap server
* Changed the missed check shop to the Dirtwater Bartender instead of Dirtwater Mercantile
    * Added price randomization for items sent to the Missed item shop, they can now show up priced anywhere from 250 meat to 1250 meat.
* Fixed location check forwarding for both `General Gob's hat` and `General Gob's Pistole` check locations if General Gob is convinced to leave without using a persuadin' feature to have him leave these items with you
* Fixed location check forwarding for `Blood Alter` at sterns ranch if you throw the goblet of blood in the jumbleneck mine's void before talking to the doll
* Fixed location check forwarding for `circus slide whistle - reward` if you happen to take all items out of the Lost and Found before placing the slide whistle in, thus locking you out of getting the reward
* Fixed location check forwarding for `cowsbane harvest` when you give the cowsbane seeds to barnaby bob instead of the guy at Lazy-A-dude Ranch
* Fixed a missed check that can occure when you collect the `x marks the spot` location without talking to halloway about his half of the map first
* Added a prevention from being able to eat the Honeyed Jellybean unless you currently have the `AntEyeVirus` flag (and thus have a need to eat it)
* Fixed a bug where the Purchase of the Honeyed Jellybean can become unpurchasable if you eat the Jellybean or progress the main quest too far, it should now be available from the point you talk to Norton and he gives you the `AntEyeVirus` or if you give him a crown.
* Fixed bypass of price for eating the turnip
* Added Misspoint for `Jewelry Shop - Specticals` if binocs are given to the blindman
    * Made the `Jewelry Shop - Specticals` check not lock out once specticals are given to the Blindman
* Added misspoint for `The Daveyard Mausoleum - The Skeleton of Dave B. Defeated` if all 4 human ashes are used from the pool for xp.
* Added Misspoint for `Desert House - Macready's Grave` if the lawyer ghost in gun mannor is killed before being sent to search for the grave.

## AP Logic Changes
* Added a new **`Clown Campsite - Circus Information`** location for the alternate free Circus Ticket route.
* Updated **Tony’s Boots** access logic so the shop can be unlocked with either normal Fort of Darkness access **or the Mushroom Map**, without placing the rest of Fort of Darkness into logic early.
* Added the missing **Shovel requirement** to `Desert House - Macready's Grave`.
* Added the **Lucky Cap requirement** to `Circus Kid - Lucky Cap Trade`.
* Updated **Curious Flat Plain - Explorer Skeleton** logic to require the **El Vibrato Transponder**.
* Split the three generic **El Vibrato Cylinders** into three location-specific progression items:
  * `El Vibrato Cylinder (Lost Dutch Oven Mine)`
  * `El Vibrato Cylinder (Curious Flat Plain)`
  * `El Vibrato Cylinder (Curious False Mountain)`
* Updated El Vibrato logic to use the appropriate location-specific cylinder:
  * Curious Abandoned Well facility access now requires the **Lost Dutch Oven Mine** cylinder.
  * Curious Flat Plain machinery/checks now require the **Curious Flat Plain** cylinder.
  * Curious False Mountain Chronokey fabrication now requires the **Curious False Mountain** cylinder.
* Updated **El Vibrato Quest Completion** logic to require all three location-specific cylinders along with its existing progression requirements.
* Reclassified **The Worst Gun** from `useful` to `filler`.
* Restored Vanilla name scheme `Kaye Ridge Mine` from `Kole Ridge Mine` due to the location being named after the developers friend and they changed their name so the developer changed the locations name to match.
* Renamed several **El Vibrato locations** to include the corresponding overworld entrance/location in the check name, making it easier to tell which ruin, storage room, construction facility, or control center a check belongs to.
* Added new check for crafting the year supply of dynamite 
* Added check location for hellstrom ranch Lucky horseshoe.
* Updated several classifactions on items to allow for more flexability on location exclusions.
* Updated check location name for `Deepest Delve Mine (Level 2) - Bracelet` to `Deepest Delve Mine (Level 3) - Bracelet`
* Changed `Human Ashes X2` from 1 bunch in pool and many more possibly in pool as needed to fill in extra space to 2 bunches in the pool to allow for missed check forwarding.