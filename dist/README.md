# Prebuilt Bicicreta

A compiled build, put here only so it can be downloaded without installing a .NET SDK.
This branch exists for that download and is not meant to be merged; the source of truth
is `cursor/valheim-mod-scaffold-020c`, where `artifacts/` stays git-ignored.

| File | Use |
| --- | --- |
| `Bicicreta.dll` | Manual install: drop it in `BepInEx/plugins`. |
| `Bicicreta-0.7.0.zip` | Thunderstore layout, for importing into a mod manager. |

Built in Release configuration from commit `5ccf629` of
`cursor/valheim-mod-scaffold-020c`. These exact bytes were then loaded into the Valheim
dedicated server under BepInEx, which registered the creature and the hammer piece,
assembled all seven parts of the bicycle, and applied all six patches with no exceptions.

The commit is recorded inside the assembly too: the .NET SDK embeds the source revision
in `AssemblyInformationalVersion`, so a build can always be traced back to its source,
and rebuilding at a different commit changes the file even when no code changed.

## What changed in 0.7.0

The wheels roll as you ride, at the speed the bicycle is actually travelling, and they
roll backwards when it goes backwards.

Nothing extra is sent over the network for this. The game already publishes a creature's
velocity to everyone through its ZDO, so every client works the rotation out for itself
from something it can already see. The bicycle still walks along with a lox's animation
underneath, which no amount of wheel spinning will fix.

## What changed before that

0.6.0 sat the rider on the seat rather than above it, and gave them something to hold.

The frame was as wide as the cart it is borrowed from, which is far wider than anything
a rider sits astride, so it has been taken in across without changing its length. The
seat and the wheels keep the widths they had, so the seat now overhangs the frame the way
a saddle does.

The rider was left hanging 0.14 m above the chair. The riding pose turns out to carry
their weight a shade *above* their root rather than below it, which 0.5.0 had the wrong
way round, so the attach point now sits just under the seat pan.

The handlebar is three boxes of wood: a post standing on the front of the frame, a short
neck reaching back from the top of it, and the bar itself across the rider's hands. A
plain T would not do, because the rider's hands come to rest above their knees, well
behind the front of the frame, so a post directly under the bar would have to rise out of
the middle of the frame.

0.5.0 sat the rider in the seat. The seat was `piece_chair`, which despite the name is the
stool, and it was built at the attach point, which is not where the rider appears to be:
the game puts their root exactly on it, and a character's root is at their feet, so the
riding pose carries their weight backwards and the stool ended up in front of them
like a set of handlebars. It is now the actual chair, `piece_chair02`, taken in and
placed by its own seat pan so the pan lands under the rider.

The bicycle is longer for it. The frame has to reach under the chair, and a wheel stands
taller than the underside of the frame, so the rear wheel only clears both by sitting
entirely behind the frame.

0.4.0 stopped it breaking things. A lox carries `lox_bite` and `lox_stomp`, and the
stomp does 100 chop and 100 pickaxe damage, enough to fell trees, break ore and flatten
a building; it also carries a `RunHitDamager` that hits whatever it runs into. The
weapons are gone, since a bicycle has no reason to bite or stomp. Running into things
stays, restricted to living targets, with the reach cut from a lox's 4 m to the length
of the bicycle.

0.3.0 gave the bicycle a seat: the vanilla wooden chair, borrowed the same way as the
wheels and frame and placed exactly where the rider sits. Before that the rider sat on
the frame's bare top edge.

It also renamed the config key `UseCartModel` to `UseStandInModel`, since the stand-in
model is no longer just the cart. An existing config file keeps the old key, which is
ignored; the new one is written with its default on first load.

0.2.0 fixed everything reported against 0.1.0: it can be ridden by aiming anywhere on
it and pressing use, it is drawn as a bicycle rather than sitting invisible underground,
it cannot be petted or renamed, it is silent, and it is bicycle-sized, so the rider sits
on the frame rather than at lox height.

## Before it will load

[Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/) 2.30.0 and
[BepInExPack_Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
have to be installed. Installing Jötunn with a mod manager pulls BepInEx in with it.
Without Jötunn the plugin is skipped, and BepInEx logs the missing dependency.

Because it adds content, it has to be installed on the server and on every client.
Replacing an older build means deleting the old `Bicicreta.dll` first if a mod manager is
not doing it for you.

## Checking what you downloaded

```
SHA-256  Bicicreta.dll         0e328a10c13b8dbcbcaf7a8506c4fe2e32f333f8d79ad235121bf13e061137b2
SHA-256  Bicicreta-0.7.0.zip   544c0968634b8a54e8c0cd7b410118fa568c79162eb2374db1327b645b4c1396
```

See the repo README for how to build one of these yourself, and for the in-game steps to
build and ride the bicycle.
