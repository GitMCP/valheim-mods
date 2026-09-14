# Changelog

## 0.3.4

A job is a standing assignment, not a one-off. Told to chop wood they keep chopping:
walk further as the nearest trees go, take a full bag home to the chests, mend a worn
axe at a workbench, and come back. Same loop for mining and gathering. They stop when
you give them a different order.

## 0.3.3

Chopping a tree now means the whole tree. They split the logs that fall, pick up the
wood, and only then walk to the next standing trunk.

## 0.3.2

Pressing Use while they are working now actually stops them and puts them at your heel.
Waiting is milling about, not standing to attention. Storing in chests can be asked
again after it has already been done. Crouch and Use, or tell them they are dismissed,
and they drop what they are carrying and leave. Pickaxes no longer dig a hole under
their own feet.

## 0.3.1

They walk on their feet, their pack opens like a chest, and they will put things in
chests and take them out when told.

The player rig they borrowed starts lying down, as if getting out of bed, and the only
code that ever stood it up was skipped so they would not count as a second player. They
now get up as soon as they exist, and telling them to follow no longer yanks the
simulation onto whoever spoke, which is what made them glitch and fall over.

What they are carrying is yours to look at. Hold the alt key and Use to open their pack
the way you open a chest, and move things in or out by hand. They will also pocket
whatever you drop at their feet.

Hauling understands a named thing: "put wood in the chest", "get iron from the chest".
Unnamed, they still tidy the yard and sort like with like.

## 0.3.0

Retainers are people now, and they no longer arrive already armed.

They use the player model — random face, hair, beard, skin — and the player's own
animations for axes, pickaxes, swords and bows. Armour they are given is worn, shown,
and counted: a bronze plate on a retainer stops as much as it does on you.

They spawn with empty pockets. An axe for chopping and a pickaxe for mining have to be
handed to them, and they will say so if you send them to work without one. They pick up
what they drop the way you do, into a proper inventory, and they will not stand at a
birch with a stone axe forever: anything too hard for the tool they have is skipped.

## 0.2.0

Retainers are hired rather than built, and there can be several of them.

The muster post is gone. In its place is an **outpost**: a banner post you build once and
then hire at, for coins, as often as you can pay, up to a limit the server sets. Each
retainer signs a contract naming who it works for and where home is, which is what makes
the rest of this possible — everyone gets a name of their own, the outpost knows how many
you already keep, and nobody can ring for somebody else's household.

A **muster bell** calls every retainer in your service back to it, wherever you left
them, and where you ring it is where home now is.

Orders are given in a **window of their own**, on a key, instead of in the debug console.
It names who is listening and keeps what was said.

An unordered retainer no longer stands to attention on the spot it was left. It drifts
around home, gravitates to the fire and the chests and the workbench, stands about, and
occasionally says something.

Seven new things to be ordered to do, on top of following, guarding and chopping:

| Order | What it does |
| --- | --- |
| Explore | Ranges around you and uncovers ground on **your** map, and nobody else's. |
| Gather | Picks berries, mushrooms and herbs. You can say which. |
| Mine | Breaks ore deposits and carries the metal back. You can say which. |
| Farm | Sows seeds out of your chests and lifts crops that are ready. |
| Cook | Puts raw food on the fires, keeps ovens fuelled, takes food off before it burns. |
| Hunt | Kills animals or monsters and collects what they drop. You can say which. |
| Haul | Picks up what is on the floor and sorts the chests. |

Orders that are about a particular thing can name it — "gather raspberries", "mine
copper", "hunt boar" — and the name is matched against what the thing is called in the
game files, what it drops, and what that is called on your screen, so whichever of the
three you happen to say works.

Every retainer now carries a pickaxe as well as an axe. Which axe and which pickaxe is a
server setting, and their tool tier is what decides how much of the world a retainer can
touch, exactly as it does for you.

The `hird` console command still works and now speaks the full vocabulary.

## 0.1.0

First build: a retainer cloned from a dvergr, a muster post to raise one, and four
orders — wait, follow, guard, chop wood — given as sentences typed in the console.
