# CS2InstantDefuse 1.3.0  
  
Implementation of a Instant Defuse plugin for CS2 using CounterStrikeSharp  
<https://docs.cssharp.dev/>  
  
This plugin allowes the bomb to be instantly defused if the following criteria is given  
- No enemy alive
- Enough time is on the bomb timer left
- No he or molotov/incendiery are currently thrown
- No inferno (flames of molotov/incendiery) are close to the bomb
- Additional Kit Checks if a player without defuser starts defusing if not enough time is left

---
# reuirements:  
- min. CounterStrikeSharp API Version: 228  

---
# installation:  
Extract the `addons` folder to the `/csgo/` directory of the dedicated server.  

---
# special thanks:  
[B3none](https://github.com/B3none): This plugin is based on [his instant defuse plugin for CSGO](https://github.com/B3none/csgo-retakes-instadefuse)
