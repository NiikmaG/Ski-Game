# Ski Game — Unity Project

A small ski racing game made in Unity 6 for a university assignment. The player skis down a slope, goes through gates and tries to finish as fast as possible. Hitting obstacles adds a time penalty.

## Controls

- **A / D** — steer left / right
- **P or ESC** — pause menu

## What's in the game

The base scene (slope, player, camera) was provided. I added the following on top of that:

- **Obstacle system** — objects tagged "Obstacle" detect when the player hits them and trigger a penalty + short knockback
- **Snowmen** — break apart on collision (chunks fly off)
- **Gate system** — flags are automatically detected and sorted into Start / Gate / Finish. Going through all gates is required for a clean run
- **Race timer** — starts when you pass the start gate, stops at the finish
- **Missed gate penalty** — if you skip a gate, +5 seconds gets added to your final time
- **Best time** — saved between sessions using PlayerPrefs. Shows on screen and updates if you beat it
- **Leaderboard** — top 5 times saved locally
- **Pause menu** — opens with P or ESC, has volume sliders for music and sound effects, restart and quit buttons
- **Hit sound** — plays when you crash into something
- **Next gate indicator** — small arrow showing which gate to aim for

## Built with

- Unity 6 (6000.4.10f1)
- Universal Render Pipeline
- TextMeshPro
- Unity Input System

## Author

Nikita Magamedgadzhiev
