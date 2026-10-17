# Catan (MVP)

A console version of Settlers of Catan: you vs. 1-3 bots.

## Play in a browser

Open `web/index.html` in any browser. Click a corner or edge on the board to build.

## Run in the console

Needs the .NET 8 SDK.

```
cd Catan
dotnet run                 # asks how many bots
dotnet run -- --bots 3     # 3 bots
dotnet run -- --seed 42    # same board and dice every time
dotnet run -- --watch      # bots only, just watch
```

## How to play

- Corners have numbers (00-53). Hexes have letters (A-S).
- `S2` = Player 2's settlement, `C2` = city. A `2` on an edge = Player 2's road.
- More dots under a number = it gets rolled more often.

Turn commands:

| Command | What it does | Cost |
|---|---|---|
| `road A B` | road between corners A and B | Wood, Brick |
| `settle N` | settlement on corner N | Wood, Brick, Sheep, Wheat |
| `city N` | upgrade your settlement on N | 2 Wheat, 3 Ore |
| `trade X Y` | give 4 X to the bank for 1 Y | |
| `spots` | list where you can settle | |
| `map`, `help`, `end` | | |

## What's in

- Random board, standard number tokens (6 and 8 never touch)
- Setup in snake order, starting resources from 2nd settlement
- Dice, production, cities make 2
- Roll 7: discard half if over 7 cards, move robber, steal
- Roads, settlements, cities, distance rule, piece limits
- Longest Road (2 VP), first to 10 VP wins
- 4:1 bank trades
- Greedy bots

## Not in yet

Development cards, Largest Army, ports, and trading between players.
