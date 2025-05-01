# Mechanical / Logic Changes
The following is a list of changes the WOLAP mod makes to the base game to facilitate and try to smoothen the West of Loathing Archipelago experience.

This list is subject to change.

## Class Changes
- Several changes have been made to reduce or remove class-specific mechanics and logic that would affect checks and item availability
- Regardless of class, you are now always given the briefcase full of snakes by your dad, and you can always collect venom and medicine from snake combats
- All class' skillbooks are randomized into the item pool so you can learn those skills for any class
- Winstreak enemy stat scaling for combat encounters is now determined by your highest stat, not your class
  - Enemies normally increase their stats based on your winstreak in that region, with the "primary" stat for your class (Muscle/Mysticality/Moxie) receiving a larger boost.  This primary stat is now determined by whatever your highest is, not which class you pick at the start, since you can learn skills from any class and may choose to build in a different direction.
- You can now discover all three crafting skill locations (Old Medicine Show, The Great Garbanzo's Hideout, Hellstrom Ranch) regardless of class
  - These are just random events and not forced like the single crafting location discovery event was previously, so it may be harder to find them all
  - Hellstrom Ranch is still discoverable normally as well, aside from the "crafting location discovery" event
  - The leatherworkery barn at Hellstrom Ranch is accessible regardless of class
- All of the class' persuasion skills (Intimidatin', Outfoxin', and Hornswogglin') have been combined and replaced with one new skill, **Persuadin'**.  Levels in Persuadin' count for all of the original skills and allow you to choose those options in dialogue.

## Global Mechanics
- Hard mode will be implemented as a toggle in the settings, so you no longer choose that when skipping the tutorial [NOT YET IMPLEMENTED - Hard mode is currently disabled]
- Auto-levelling has been disabled by default (it can still be toggled on in the settings)
- In the base game there are many, many item drops that can easily be missed and permanently locked out of.  Where possible these have been tweaked to be *less* missable, but it's not reasonable to make it impossible in all cases. A system has been implemented so that completely missed checks can later be purchased from a shop.
  - The plan is to create a new custom shop for this in the future, but for now these checks are just sent to Dirtwater Mercantile

## Your Family's Farm (Starting Zone)
- The gifts you receive from your parents are no longer class-dependent or by choice, they're just one check location each
  - Dad's check is in addition to always receiving the snake briefcase
- You now have to talk to your Mom (not just your Dad) before leaving the family farm, so you can't miss the check from her - and to not be rude
  - This also prevents you from missing out on the checks that replace the other "books" you don't get from her.  These would normally be split up between Breadwood Trading Post and Buttonwillow's Store, and wouldn't spawn if you skipped your Mom completely.  They are now guaranteed shop checks.
- You are now forced to skip the Boring Springs tutorial, proceeding directly to Dirtwater.  There have also been several changes to the process of skipping the tutorial:
  - Hard mode is no longer chosen here
  - The choices you'd normally make when skipping the tutorial (your horse, pardner, etc.) are no longer affected by what you've unlocked in the tutorial in previous playthroughs
  - You can no longer choose Gary as your pardner (he changes a bunch of logic throughout the game, so he's currently unsupported)
  - Your horse/pardner are mostly a cosmetic choice -- the handful of cases where it would lock you out of a check, or be required for one, have been changed
  - You are granted both Honorable and Ruthless, so you're always free to choose either of those options when they appear
  - Unless otherwise specified in your YAML, you start with a set of basic gear, including:
	- All 3 classes' starting hats (four-gallon hat, barely-enchanted hat, floppy derby)
	- Deputy pistol
	- Nasty ring
	- Broken board (the old cavalry saber is randomized)
	- Replica sherf badge
	- 2 needles
	- Pair of silver cufflinks
	- Fine silver pocketwatch
	- Dusty turnip
	- 1000 meat
  - You **DO NOT** start with a shovel, crowbar, or skinnin' knife (by default)
  - If you specify a starting inventory in the YAML (from the item pool or otherwise), it will override most of the default starting gear.  Any of the EQUIPPABLE items from the default gear that aren't included in a starting inventory from the pool will be put back into the pool to be randomized -- they're excluded by default.  On top of whatever you specify, you will also start with the dusty turnip (since it's needed for the turnip crown check).
  - You are granted 50 XP
  - You are considered to have met Cactus Bill and started Curly's quest in the tutorial, but you are *not* considered to have looted the Boring Springs Saloon's spittoon -- inc ase you *really* wanna go for the Spit-Free perk (not currently randomized).

## Miscellaneous Location Logic
- Many cases which previously checked if you have a certain item in your inventory now check for a flag instead
  - Example: previously if you bought a balloon from the circus vendor and then talked to him again, he'd talk to you about other circus clown things. If you got rid of the balloon and then talked to him, you could buy it again.  Now, his dialogue will change only after buying something from him (even if you receive a balloon from the item pool beforehand) and won't change back regardless of your inventory.
- Smashing the goblet/altar at Stearns Ranch still grants the check, even though you normally only get an item from taking the goblet
- Feeding the Stripey Hat Gang to the giant spider (with Ruthless) will still grant a check, even though the stripey hat the check replaces is normally only granted from fighting them
- The checks for General Gob's hat and pistol are both available from any persuasion check, even though Intimidatin' normally only gives his hat and Hornswogglin' normally only gives his gun
- The "Fort Cowardice Goblin Caves - First Victory" check replaces the Goblin Spyglass drop from the infinite goblin combat there, and it is guaranteed the first time you beat them (unlike the spyglass, which is normally a random chance if you don't have it already)
- In Gustavson Gulch, in the theatre tent, you can now fight Goblet even if you listen from backstage first… if you're a jerk
- You can get the remaining Gustavson Gulch treasure cave key even if you've already unlocked it
- You can get the check for picking up an interesting rock in Railroad Camp (East) or The West Pole even if you've already gotten the other, as long as you've reached that step in Dr. Morton's quest
- In Fort Alldead, you can get the check for grabbing the skeleton spoor out of the toilet even if you don't have Doc Alice or the idea to poison the skeletons, and doing so will give you the idea to poison them if you don't have it already
- You can now only buy one "item" (check) from the herbal remedy hippy at Lazy-A Dude Ranch, it's not repeatable
- In the Abandoned Pickle Factory, you can now get the checks for the ghosts' unique loot (Ghost Cowboy Hat / Ghost Pistol / Cursed Pants) from solving the puzzles peacefully in addition to beating them in combat
- Technically the ticket booth at the circus doesn't normally grant an item -- paying for a ticket (or finding one elsewhere and bringing it) just gives you access to the circus.  A check has been added there for "buying a ticket" (or talking your way into one for free), and you'll need to receive the Circus Ticket item from the item pool to get in.
  - If you receive the ticket before you've bought the check, you'll still be able to buy it after you've turned in the ticket
- You can now get the check for the ticket prize from the circus games even if the performance is already over
- If you are ejected from the circus, they no longer take your coupon for a year's supply of dynamite (if you have one)
- The Ghostwood temporary visitor's permit still expires in 11 seconds, but you can no longer get a new one (as long as you've already gone through the process once for the check).  You'll just have to find that pencil.
- Can now buy the "mushroom pliers" check from Irene in Fort of Darkness even if you've already received the pliers from the item pool
- The vending machines in the Curious Abandoned Well and Madness Maw Mine El Vibrato facilities can now produce each of their options once (with each giving a check), they are no longer repeatable
- You can now get both checks for the Kellogg Grain Flakes recipe even if you've already gotten the other one or received the item from the pool
- You can now get the check for defeating the Necromancer (replacing his crown pickup) regardless of how you defeat him, you no longer need to usurp him and you can get it even with the hard hat
  - This also means that you do not need to receive and read 6 Nex-Mex books to get this check
- You can get the check for getting the right half of Curly's map from Halloway, even if you've already received both halves of the map and put it together
- Simplified progressive logic for Rufus' gift checks
  - Normally, if you've gotten your class' crafting skill, your next gift from Rufus will always be the relevant crafting bench.  This means that it's theoretically possible to miss that gift if you use all 5 postcards before getting the skill.  The animal hat gift can also be missed in vanilla if you don't shoot a stuffed animal during character creation to choose your favorite.  In WOLAP the gifts are all just given in sequential order, one check each, with the check replacing the crafting bench given first.
- The logic and increments for some progressive container loot/checks have been tweaked
  - Example: The logic for grave container loot seems like it may have been intended to give incremental items starting with the 3rd or 4th container, but since the flag was incremented at the end of the script it actually started with the 5th (5, 8, 11…).  In keeping with the pattern of 3, it's been changed to start with the 3rd container.
  - Also, you would normally always be given an old wedding ring if you didn't have one, unless you were also eligible for an incremental item -- in which case you'd randomly be given one of the two and would get the other from the next container.  Now, you'll just get both at once.
- For the House-in-the-Desert Gang:
  - After locking them in and talking to them, you are now given another chance to shake them down after choosing the "hands up" or "we've got you surrounded" options
	- After choosing both of those, you can still just arrest the gang without shaking them down (a new Honorable option), which gives some bonus XP but misses both of the gang's checks - they'll be available from the shop afterward
  - After shaking down the gang, you're now given the option to still consider burning down the house. You can also choose to fight them at this point now too. Either way you'll get the check for killing them (or you can just arrest them).
- It's no longer possible to completely miss out on Breadwood by building a bridge before talking to Smee at the second railroad camp, he now unlocks it on your map in that case
- The coordinates of some map locations around Buffalo Pile have been tweaked to make room for Old Medicine Show, so it's not stacked on top of The Great Garbanzo's Hideout

## Random Encounters
- You no longer need to have the Pale Horse to get the progressive encounter for a check replacing a Nex-Mex skillbook (13th encounter)
- For many random encounters where combat is an option, their unique items (checks) will now also drop from combat if they didn't already
- Region A:
  - You no longer need to have the Crazy Horse (and neither Stupid Walking nor Walking Stupid) to get the check for the abandoned stagecoach Walking Stupid encounter
  - During the encounter with the corpse of a cowboy you can now get the check for the chaps even if you honorably bury the remains
  - During the encounter with the skeleton of a beanslinger, you can get the check for the bean-stained pants even if you've already received them.  The guaranteed drop for a cowboy chef's chat from the Hornswogglin' option is also replaced by this check.
- Region C:
  - During the encounter with the grizzled skinner you can get the check for the varmint skinnin' knife drop even if you already have one
  - You can choose which ring(s) you want to buy from the Gulch Goblin Jeweler even if you don't have Goblintongue, you can buy multiple rings in a row, and you can fight him even after buying rings.  This is one case where not all checks drop from combat - if you want to get all the checks during this encounter, you'll have to buy his 3 wares and then fight him.
- Region D:
  - You can get the check for the Ring of Gettin' Places Faster even if you have the hard hat (since hard mode is going to be an in-game toggle)
  - You can get the encounter for the drunk ghost even if you don't have the Pale Horse, and you can get the check for giving him ghost whiskey even if you already have the ghost ring
- Region E:
  - During the wandering cultist encounter you can get the check for the black hood drop from combat even if you already have a black hood
- Region G:
  - You can buy multiple sculptures in a row from the Goblin Driftwood Sculptor, and you can fight him even after buying things.  This is another case where not all checks drop from combat - if you want to get all the checks during this encounter, you'll have to buy at least the weapon and hat and then fight him (can also buy the shield, but it drops from combat by default).
  - The drops for the sea skull and damp autograph are combined into the same check for the seaweed-draped skeleton encounter
- Region H:
  - During the barista encounter, you can now get both checks - you can fight even after deciding to buy and keep the coffee, and you get both checks from winning if you threw the coffee at her

## DLC
- The Gun Manor difficulty flag is now set upon character creation and forced to a value of 5
  - This is normally set when you first visit Gun Manor based on your progress in the main game at that point.  Difficulty 5 normally indicates progress to Region E, so about mid-level.
  - This is necessary to support receiving DLC items from the pool, many of which are enchanted with effects that are scaled based on the difficulty flag
  - This difficulty value also affects the difficulty of stat checks, skill requirements, and some combat in Gun Manor
  - In the future there may be an Archipelago or in-game option to set or change this difficulty
- In the Gun Manor Laboratory, you can get the check for crafting the bismuth bomb even if you've already defeated the GHOST
- In the Gun Manor Laboratory, you can do the check for crafting a silenter once (as long as you've received the components), but to repeat the process and actually craft another silenter you'll need to receive (and use) the one from the item pool first
- In the Gun Manor Kitchen, you can get the check for grabbing a bean pan from the shelf even if you're not a beanslinger
- In Mr. Gun's Manliness Room, you can interact with the humidor once to get the check, but after you've received the one cheap cigar from the item pool (to make an exploding cigar for the novelty salesman ghost), then you will be able to endlessly interact with the humidor as normal to grab more
- You can fight Mrs. Gun even if you have Honorable (which you do by default now)