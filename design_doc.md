
### File 1: Final Game Project Proposal (Section 4 Update)

*You can replace your existing Section 4 with this version. It now incorporates the new fishing-deck mechanic and addresses the asset challenge directly.*

#### 4. New Knowledge and Open Questions

To successfully deliver this project within the time constraints, we must overcome distinct implementation learning curves in Godot and a significant asset management challenge. While we are confident in our foundational Godot programming skills, combining an active, timing-based action game (fishing) with a data-driven, turn-based card system introduces complex new architectural and design challenges.

**Technical Knowledge and Learning Requirements:**

* **Action/RPG Hybrid Architecture:** This is our primary architectural challenge. We need to learn how to seamlessly transition between two entirely different game states: the active "Fishing Phase" (requiring precise input and physics) and the turn-based "Boss Combat Phase" (requiring rigid logic).
* **Active Fishing Mechanics (Godot Physics/Input):** We must master Godot’s collision detection and input handling to create an aiming-and-timing mechanic where a moving hook must "catch" specifically moving fish.
* **Data Management (Resource-Based Data):** For game balance, we plan to implement a data-driven card system. This will involve learning how to define custom C# classes and map them directly to Godot `.tres` (Resource) files for streamlined content creation (e.g., mapping a "red fish" to "Fire Damage").
* **Decoupled Architecture (Signal Bus):** To prevent monolithic code, we will implement a centralized "Global Signal Bus" pattern. We need to learn best practices for this pattern to manage complex combat events (e.g., HP changes, card discards) without tightly coupling game systems.
* **Roguelike Design Patterns:** We need to research and understand how other turn-based roguelikes manage complex battle state machines.

**Assets and Tools:**

* **Asset Pipeline and Tools:** As a team without a dedicated artist, a major risk is sourcing or creating necessary visual assets (sprites, fishing animations, card UI). We will devote initial time to exploring user-friendly pixel-art tools or identifying existing asset libraries with permissive licenses.

**Open Questions and Risks:**

* **Complexity Management (Combat Math):** A major concern is the implementing of "Status Effects" (like Poison or Bleed). We need to determine if tracking these variables adds too much mathematical overhead or implementation complexity for our 30-hour timeframe. We may simplify this to straightforward attack/defense damage.
* **"Fun Factor" of Fishing:** The primary risk is making the active fishing phase engaging, non-repetitive, and skillful, rather than just tedious. We must quickly iterate on the fishing mechanic (moving cursor vs. mouse-aiming hook) in our MVP to prove the concept.
* **Asset Sourcing Blockers:** Since none of us are confident artistically, we have a contingency plan: If asset sourcing or creation becomes a major blocker, we are prepared to shift to a purely text-based card UI or use basic "geometric/abstract" shapes (colored circles/rectangles) for the fish and assets to focus development purely on gameplay and polish.

***

### File 2: MVP Definition (The "What")

*This document defines the minimum feature set needed to prove the concept works. Focus 100% on this first.*

# MVP (Minimum Viable Product) Definition

**Core Gameplay Loop (MVP):**

1.  **Fishing Phase (30 seconds):** Fish appear and move. The player aims/times a single hook to catch 1 fish.
2.  **Deck Conversion:** The 1 caught fish is added to a 1-card player deck.
3.  **Combat Phase (Turn-Based):**
    * Boss plays an attack.
    * Player plays their 1 card (e.g., simple damage).
    * Discard card; reset turn.
4.  **Victory/Loss Check:** Check HP. If neither is zero, repeat loop from Phase 1 (re-fishing for new cards).

**MVP Features Checklist:**

**1. General & UI (Godot State & Scene Management)**

* [ ] Basic Godot project structure.
* [ ] Main Menu (single "Start Game" button).
* [ ] Transition Logic: Seamlessly moving from "Fishing Scene" to "Combat Scene."
* [ ] Core UI: Simple Health Bars for Player and Boss.
* [ ] Game Loop State Machine (Initialize $\rightarrow$ Fish $\rightarrow$ Convert $\rightarrow$ Combat $\rightarrow$ Victory/Loss).

**2. Fishing Phase Scene (Action)**

* [ ] **Static Scene:** A basic underwater/water background and a "surface."
* [ ] **Fish:** Three static or simple-patrolling (left/right) "Fish" sprites.
* [ ] **Player Hook:** A hook sprite controlled by the player (e.g., follows the mouse cursor's X-axis, snaps down on click).
* [ ] **Catch Mechanic:** A `KinematicBody2D` or simple `Area2D` collision detection to "grab" the first fish the hook touches.
* [ ] **State Check:** Ensure only one fish can be caught per "throw."

**3. Deck/Card Conversion Logic (Data)**

* [ ] **Fish $\rightarrow$ Card Map:** Simple hardcoded mapping (e.g., Catch Red Fish $\rightarrow$ Add "Fire Strike" C# class to the active deck).
* [ ] **Core Card Data:** Define basic properties (Card Name, Type, Damage, and maybe a single Status Effect).

**4. Boss Combat Phase Scene (Turn-Based)**

* [ ] **Sprites:** One static Boss sprite and one static Player sprite.
* [ ] **Hand UI:** Simple HBox or Button UI to display the 1 caught card and allow the player to click it to "play."
* [ ] **Turn Logic:**
    * [ ] Define Boss Action (e.g., Boss always deals 5 damage).
    * [ ] Player Action (click card $\rightarrow$ execute effect $\rightarrow$ subtract HP $\rightarrow$ update UI).
* [ ] **Card Management:** Simple discard pile logic (e.g., move the card object out of the "Hand" HBox).

***

### File 3: Design Document "Whys"

*This document explains the rationale behind your design decisions, useful for your final project narrative.*

# Project Whys: Design Rationale

This document tracks *why* our team made specific design choices for the "Fishing-Deck Roguelike."

**1. Why use an Active Fishing Phase for a Card Game?**

* **Skillful Expression:** Most deckbuilders rely on rng for card acquisition (loot tables). By making the acquisition phase an active skill game (timing/aiming), we shift some of the core skill from *just* playing the deck to the *building* of the deck.
* **Unique Narrative Hook:** Combining fishing with magical cards creates a distinct, memorable premise that sets our game apart from standard fantasy or sci-fi roguelikes.

**2. Why focus on a Timing/Aiming Game for Fishing?**

* **The "Gambla" Element:** Players can see the potential rewards (different colored fish) swimming by. They must skillfully time their input to grab the *exact* one they want. This adds high-risk, high-reward tension to the deck-building phase.
* **Godot Strength:** This mechanic utilizes Godot's core strengths (handling physics, collisions, and user input), making it highly feasible for an MVP implementation.

**3. Why use hardcoded Colored Fish for early data mapping?**

* **Scope & Simplicity:** Developing 10+ detailed fish and card assets immediately is outside our 30-hour workload. By using simple color coding (Red = Fire, Blue = Shield), we can build the core systems and logic *now* using simple `ColorRects` or placeholder sprites, leaving the complex art and data pipeline for later optimization or stretch goals.

**4. Why is the MVP Scope so Narrow (1 Fish, 1 Deck)?**

* **Prove the Core Hook First:** We cannot afford to build a complex 12-card system until we know that:
    1.  We can build the active fishing game.
    2.  We can convert the result of the fishing game into card data.
    3.  We can transition between the two game modes without errors.
    This narrow MVP confirms these core engineering pillars are stable before we expand the scope.

**5. Why exclude complex features (Status Effects, Multi-Enemy, Fishing Modifiers) in the MVP?**

* **Time Management:** Features like complex math for status effects (bleed/poison over time) and additional enemy types add geometric implementation overhead. By focusing only on HP, basic damage, and single-card-hand management, we prioritize a working, polished core loop over unproven, bloated features. These are all natural candidates for stretch goals.

**6. Why create an Art Contingency Plan?**

* **Team Skill Risk:** As a team lacking a confident artist, sourcing or creating assets is our biggest production bottleneck. By preparing to pivot to an abstract/geometric art style, we ensure we will have a functional, playable game for the final deadline even if we cannot produce high-quality sprites.
