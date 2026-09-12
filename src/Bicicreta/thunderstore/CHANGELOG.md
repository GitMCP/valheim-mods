# Changelog

## 0.7.0

The wheels roll as you ride, at the speed the bicycle is actually travelling, and they
roll backwards when it goes backwards. Nothing extra is sent over the network for this:
the game already tells every client how fast a creature is moving, so each one works the
rotation out for itself.

## 0.6.0

The rider sits on the seat instead of hovering above it, and has something to hold: a
handlebar built from a post standing on the front of the frame, a short neck reaching
back from the top of it, and the bar itself across the rider's hands.

The frame has been taken in across. It was as wide as the cart it is borrowed from, which
is far wider than anything a rider sits astride. The seat and the wheels keep the widths
they had, so the seat now overhangs the frame the way a saddle does.

## 0.5.0

The seat is the wooden chair, and it is underneath the rider rather than in front of
them. The bicycle is longer for it: the frame has to reach under the chair, and a wheel
stands taller than the underside of the frame, so the rear wheel only clears both by
sitting entirely behind the frame.

## 0.4.0

It no longer damages buildings, trees, ore, carts or ships, whether you ride it into them
or park it next to a fight. A lox bites and stomps, and the stomp is strong enough to
fell a tree or flatten a wall; a bicycle has no reason to do either, so both are gone.
Running into things stays, restricted to living targets, with the reach cut from a lox's
4 m to the length of the bicycle.

## 0.3.0

The bicycle has a seat. Before this the rider sat on the frame's bare top edge.

The config key `UseCartModel` is now `UseStandInModel`, since the stand-in model is no
longer just the cart. An existing config file keeps the old key, which is ignored; the
new one is written with its default on first load.

## 0.2.0

It can be ridden, by aiming anywhere on it and pressing use. It is drawn as a bicycle
rather than sitting invisible in the ground. It cannot be petted or renamed, it is
silent, and it is the size of a bicycle rather than of the lox it is built from, so the
rider sits on the frame instead of high in the air.

## 0.1.0

First build: a hammer piece, a recipe, and something that appears when you place it.
