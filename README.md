# Brimstone
**A High-Performance Hearthstone simulator in C#**

Brimstone is a high-performance Hearthstone simulator written in C#, designed to make it simple and fast for developers to experiment with AI, Machine Learning, game formats, card balance and new cards and mechanics.

Main features:

* High performance
* High level of parity with actual Hearthstone behaviour
* Tag-driven, tick-based design with flexible hooks and infinite undo*
* Extremely fast state cloning - up to 400,000 games/second**
* Multi-source game state importing*
* Game tree creation and traversal
* Probabilistic state outcome generation
* Fast parallel tree search - calculates all possible board states and probabilities for 10 random missiles on a full game board in under 1.5 seconds @ 75,000 branches/second***
* Declarative card definition language and easy-to-use API
* Connectors for CNTK/TensorFlow neural network packages*
* Extensibility features for other collectible card games*
* Easy black box installation and usage
* Great documentation

Web site and documentation: http://hearthsim.github.io/Brimstone

Download: https://github.com/HearthSim/Brimstone/releases

Developer community: http://discord.me/hearthstoneworkinggroup

**NOTE:** Brimstone is under heavy development and is not a finished product.

(*) Infinite undo, multi-source import, NN connectors and extensibility not yet implemented

(**) Parellel copy-on-write cloning, no backing store; Intel Core i7-2600K, 16GB RAM, Windows 10 64-bit

(***) Parallel breadth-first tree search with backing store, 5 Bloodfen Raptors + 2 Boom Bots per board side, Boom Bots on the right; clone game, add tree node, perform a game action, equivalence test and prune; 106,945 branches (63,041 pruned), 2170 unqiue game states in 1.48 seconds; Intel Core i7-2600K, 16GB RAM, Windows 10 64-bit

Developed by The Hearthstone Working Group - part of http://hearthsim.info
