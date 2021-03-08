# Commands  
`/troll {player}` - Adds a player to the troll list (use full name or id)  
  
`/untroll {player}` - Removes a player from the troll list  
  
`/trolls` - View the troll list  
    
`/chicken {player}` - Forces player into a chicken  
  
`/sit {player}` - Admin sit for player (invisible chair)  
  
`/unsit {player}` - Stops admin sit  
  
`/landmine {player}` - Spawns a landmine under a player and blows up instantly  
  
`/freefall {player}` - Sends a player into air to fall back down, pauses flyhack  
  
# Permissions  
`admintroll.use` - Grants access to all commands.  
  
# Default Configuration  
```  
{  
  "Permission": "admintroll.use", - Permission to use for commands  
  "Prevent Looting": true, - Prevents a player from looting anything (recyclers, furnaces, corpses, containers)  
  "Spawn Landmine On Loot": false, - Spawns a landmine under the player when they attempt to loot something, killing them; this does not damage nearby entities or players  
  "Drop Player's Active Item On Attack Chance": 50, - Percent to drop their gun while shooting (0 for none, 100 to always drop)  
  "Drop Gun On Shoot Message": "", - Message to display in chat when they drop their gun  
  "Fail Reload Chance": 50, - Percent to fail reload (0 for none, 100 to always fail)  
  "Drop Weapon On Fail Reload": true, - Drop player's weapon on fail reload  
  "Fail Reload Message": "", - Message to display in chat when they fail reload  
  "Cannot Loot Message": "Cannot open container as it does not exist on the server.", - Message to display in chat when a user cannot open a container  
  "Troll Teammates": false, - When /troll is used, apply it to the user's teammates  
  "Instantly Dud Satchels": true, - Instantly dud satchels thrown by trolled users  
  "Freeze Rockets": true, - Freeze rockets shot by trolled users for time  
  "Non Sticky Satchels": true, - Satchels thrown by trolled users won't stick to walls and fall off  
  "Trolled Players Cannot Damage": true, - Trolled players cannot deal damage to other players, animals, or entities (Highly recommended)  
  "Trolled Players Damage Karma": true, - Damage the player when they damage someone else or an entity  
  "Damage Karma Amount": 10, - Amount of damage to apply  
  "Can Farm Nodes/Trees": false, - Trolled players cannot farm resources  
  "Cannot Gather Toast On Fail": true, - Shows "You cannot gather anything here." rust toast when they farm a node or tree  
  "Cannot Farm Message": "", - Message to show in chat when trolled player farms  
  "Can Collect Cloth": false, - Disallows/allows collecting cloth by trolled players  
  "Cannot Take Cloth Message": "", - Cannot take cloth message  
  "Cannot Craft Items": true, - Prevent crafting by trolled players (InstantCraft may still craft it)  
  "Replace Crafted Item With Horse Dung": true, - Replace crafted item with horse dung  
  "Cannot Craft Message": "", - Message to show when craft is attempted by trolled player  
  "Cannot Heal": true, - Prevents trolled players from using med syringes, medkits, or bandages  
  "Cannot Heal Message": "", - Message to show on heal attempt  
  "Cannot Build": true, - Prevents trolled players from building  
  "Cannot Build Message": "", - Message to show on build fail  
  "Cannot Upgrade": true, - Prevents upgrading (Being reworked)  
  "Cannot Upgrade Message": "", - Message to show on upgrade attempt  
  "Persist Trolled Player List On Restart": true - Persists the troll list on server restart  
}  
```  
  
.gg/gEvgSF7v2s
