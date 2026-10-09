# BigFriendlyButton

**Confirm your extraction with a button. Host only, nobody else needs it.**

![The button filling up, with the mod and without it](https://raw.githubusercontent.com/VirtualPixel/BigFriendlyButton/main/media/tour.webp)

Extraction doesn't go the second you hit the goal anymore. It waits till somebody presses the button, and you can press it again to cancel. No more extraction going off while you're still carrying the last valuable in.

Don't want a button? Install it anyway with Enabled off. You play vanilla, keep the progress bar if you want it, and you still get the button when you join someone hosting another extraction button mod.

## What you'll see

**With the mod**, the shop's red button on its stand, right next to the extraction:

![The stand button: not enough yet, getting there, ready](https://raw.githubusercontent.com/VirtualPixel/BigFriendlyButton/main/media/with_mod.jpg)

**Without the mod**, the button off a cosmetic box, squashed flat in the same spot:

![The box button: not enough yet, getting there, ready](https://raw.githubusercontent.com/VirtualPixel/BigFriendlyButton/main/media/without_mod.jpg)

- **Not enough yet** (left). The stand's button is dark and won't press, its bar just shows a little red stub. The box's bar is empty and grabbing it gets you the extraction's red X
- **Getting there** (middle). Both bars fill up yellow as you bring more in
- **Ready** (right). The button lights up red, the bars go green and the extraction screen says READY. Press it (or grab the box) and the haul goes, press again to cancel

With Send early on the stand's button is lit the whole time instead, since you can always press it.

## What it does

- **Confirm extraction.** When a point opens, the shop's red button shows up on its stand next to the pad. Press it and the haul goes (3-2-1, same as always)
- **Cancel extraction.** Press it again before the extraction goes. Nothing on the pad goes anywhere, press again when you're ready
- **Ready light.** The button stays dark till there's enough on the pad, then it lights up and the extraction screen says READY. Until it lights up it won't press
- **Progress bar.** A bar on the front of the stand fills up as you get closer to the goal
- **Send early** (optional). Send whenever you want, even with nothing on the pad. Whatever you were short goes onto the next extraction's goal
- **Auto extract** (optional). Goes on its own after a few seconds at the goal

## Who needs it

- **Only the host.** Everything runs on the host, all the settings below are theirs (except Progress bar, which is just your screen)
- **Friends with the mod** get the same shop button. It only lights up when their press would do somethign with the host's settings
- **Friends without the mod** get the button off a cosmetic box instead, squashed flat into a panel on the floor in the same spot. Its health bar fills like the progress bar. Grab it to press, too early and they get the extraction's own red X. It's bolted down, gone once the extraction's done and never counts as a real cosmetic box. (They might get the game's cosmetic box tutorial tip once)

## Settings

In `BepInEx/config/Vippy.BigFriendlyButton.cfg`, or in-game with REPOConfig (hit Save Changes). They all work live, even mid level.

- **Enabled** (on): off and extraction is vanilla on the spot, the button and the box go away. On and they're back. Other button mods keep working either way
- **Progress bar** (on): the bar on the stand, just your screen. With Enabled off it moves to the extraction screen and shows the haul against the goal
- **Only host can press** (off): turns itself off while the host is dead so the haul can't get stuck
- **Press again to cancel** (on)
- **Send early** (off): on the last extraction there's nowhere for the rest to go, so it's just gone
- **Auto extract after** (0): seconds at the goal before it goes on its own, 0 = never
- **Preview the no-mod button** (off): host only, see what friends without the mod get so you can try it solo

## Works with other button mods

- **Zehs' ExtractionPointConfirmButton:** join someone hosting it without having it and you still get his button in his spot, and it works. On or off.
- **Already have an extraction confirm mod installed** (ExtractionPointConfirmButton, AddExtractionButton etc)? This one steps aside so you don't need two presses.
- **AddExtractionButton (the clown)** and the other host only confirm mods already work for all poeple.
- **ZeroGravityExtraction:** plays nice. With Enabled off the progress bar sits right above its switch.

## Come hang out

I'm Vippy. I make R.E.P.O. mods and I read every bug report.

| | |
|---|---|
| **[Vippy's Discord](https://discord.gg/kKqhck2NrP)** | Test builds land here before Thunderstore, you get a say in what comes next, and it's the fastest way to get a bug fixed. Come say hi. |
| **[R.E.P.O. Modding Server](https://discord.gg/9fDzZ9sk95)** | The whole modding scene, not just me. |
| **[More of my mods](https://thunderstore.io/c/repo/p/Vippy/)** | Everything else I've made for R.E.P.O. |

## Keep the mods coming

Everything I make is free and stays free. Two ways to help if you feel like it, neither one expected:

- **[Ko-fi](https://ko-fi.com/vippydev)**: buy me a coffee and your name goes on the supporters list in my Discord. Every coffee buys another evening on the next update.
- **[BisectHosting](https://bisecthosting.com/vippy)**: hosting a server for Minecraft or anything else your crew plays? Code `vippy` takes 25% off, and I get a cut at no cost to you. It's where my own servers live.

[![25% off BisectHosting servers with code vippy](https://www.bisecthosting.com/partners/custom-banners/71eecea6-f5bb-437d-ac56-f6fee4266193.png)](https://bisecthosting.com/vippy)
