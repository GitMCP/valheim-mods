# Rows

Passenger seats on boats get oars. Sit in one and you help the captain go faster.

The person at the helm still steers and still sets the sail. Everyone else on a chair
is a rower: each occupied seat adds a push in the direction the helm has asked for.
An empty seat does nothing. Nobody at the helm, and the oars rest.

The oars are wooden stand-ins until there is a real mesh. They stroke while you sit
and the ship is under way.

Because this changes how a shared ship moves, it has to be installed on the **server
and on every client**.

## Installation

Drop `Rows.dll` into `BepInEx/plugins`, or install the zip with a mod manager.
Requires [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/).

## Configuration

`BepInEx/config/com.gitmcp.rows.cfg` is written on first launch. The force is a rule
of the world, so the server's copy wins.

| Setting | Default | Description |
| --- | --- | --- |
| `Rows / ForcePerRower` | `0.35` | How much one occupied passenger seat adds, as a fraction of the ship's own paddle force. |
