
This is a crucial step. To get an AI builder (or a human programmer) to generate a functional wireframe or initial scene setup in Godot, you need to provide extremely specific, structurally organized descriptions of the visual hierarchy and the interactive elements. You cannot just describe the "gameplay"; you must describe the *nodes* and *layouts*.

Here is a Design Document tailored for "piping into an AI builder." It is structured by *Scene* and focuses on the precise layout and node names (assuming Godot's standard naming conventions) needed to construct a functional UI and interaction wireframe.

***

# Wireframe Generation Design Doc: Fishing-Deck Roguelike (MVP)

## Document Purpose

This document provides structured node hierarchies and element descriptions necessary to generate a functional wireframe and initial scene setup in the Godot Game Engine for the "Fishing-Deck Roguelike MVP."

---

## 1. Global / Master Scene (State Manager)

This is the main scene that handles transitions between the Fishing and Combat states. It is a non-visual logical controller.

**Scene Type:** Node (Basic)
**Node Name:** `GameManager`
**Script:** attached `GameManager.cs` or `GameManager.gd`

* **Logic:**
    * Load/Unload child scenes (`FishingScene`, `CombatScene`).
    * Manage Player/Boss HP data (global variables).
    * Manage Deck/Discard Data (e.g., a `List<Card>` or `Array` variable).

---

## 2. Fishing Phase Scene (Action)

This scene is where the active fishing mechanic occurs. The wireframe focuses on layout and initial collision placement.

**Scene Type:** Node2D
**Node Name:** `FishingScene`

### Visual Hierarchy & Layout:

* **`FishingScene` (Node2D)**
    * **`CanvasLayer` (CanvasLayer)** *[Ensures UI elements stay on top of the 'water']*
        * **`UI_Container` (MarginContainer)** *[Anchored to Top-Right]*
            * **`HBox_Stats` (HBoxContainer)**
                * **`Label_PlayerHP` (Label)** [Text: "Player HP: 100/100"]
                * **`Label_Timer` (Label)** [Text: "Time: 30s"]
    * **`Background_Water` (TextureRect)** *[Wireframe: Blue placeholder, full screen]*
    * **`Surface_Line` (ColorRect)** *[Wireframe: Green line at Top (y=50) for 'surface' collision]*
    * **`PlayerHook_Origin` (Position2D)** *[Wireframe: Anchor point for the hook mechanism, Top-Center]*
        * **`Hook_Mechanism` (KinematicBody2D / Area2D)**
            * **`CollisionShape2D` (CollisionShape2D)** *[Wireframe: Small circle (Radius: 5)]*
            * **`Sprite_Hook` (Sprite2D)** *[Wireframe: White 'J' shape placeholder]*
    * **`Fish_Spawn_Area` (Area2D / Path2D)** *[Wireframe: A rectangular border covering the main 'water' area (e.g., 200x200 to 800x600)]*
        * **`Fish_Placeholder_Common` (Area2D)** *[Wireframe: Small Gray Rectangle/Circle (e.g., 20x10)]*
        * **`Fish_Placeholder_Fire` (Area2D)** *[Wireframe: Small Red Rectangle/Circle]*
        * **`Fish_Placeholder_Shield` (Area2D)** *[Wireframe: Small Blue Rectangle/Circle]*

### Interaction Definition:

* **Player Input:** Hook mechanism follows mouse X-axis (constrained). Mouse Click triggers Hook mechanism 'dropping' (moving down the Y-axis towards bottom of screen).
* **Collision Interaction:**
    * `Hook_Mechanism` detects collision with any `Fish_Placeholder_X` Area2D.
    * On collision: `Fish_X` is 'caught' (becomes child of the hook), fishing state ends. Trigger `GameManager` to transition to `CombatScene`.

---

## 3. Boss Combat Phase Scene (Turn-Based)

This scene is a static UI wireframe for the battle. Focus on clear layout and button placement.

**Scene Type:** Control (User Interface)
**Node Name:** `CombatScene`
**Anchors:** Full Rect (matches screen resolution)

### Visual Hierarchy & Layout:

* **`CombatScene` (Control)**
    * **`CanvasLayer_Combat` (CanvasLayer)**
        * **`Background_Dungeon` (TextureRect)** *[Wireframe: Gray/Dark placeholder, full screen]*
        * **`VBox_MainLayout` (VBoxContainer)** *[Anchored Full Rect]*
            * **`Row1_BossArea` (HBoxContainer)** *[Anchored Top]*
                * **`Margin_Boss` (MarginContainer)** *[Padding: Left]*
                    * **`VBox_BossStats` (VBoxContainer)**
                        * **`Label_BossName` (Label)** [Text: "LEVEL 1 BOSS"]
                        * **`ProgressBar_BossHP` (ProgressBar)** [Value: 100, Min: 0, Max: 100]
                * **`Control_BossSpriteAnchor` (Control)** *[Wireframe: Centered large square for Boss Sprite]*
                    * **`Sprite_Boss` (TextureRect / AnimatedSprite)** *[Wireframe: Large static rectangle placeholder]*
            * **`Row2_PlayerArea` (HBoxContainer)** *[Anchored Middle-Bottom]*
                * **`Control_PlayerSpriteAnchor` (Control)** *[Wireframe: Centered large square for Player Sprite]*
                    * **`Sprite_Player` (TextureRect / AnimatedSprite)** *[Wireframe: Medium static rectangle placeholder]*
                * **`Margin_Player` (MarginContainer)** *[Padding: Right]*
                    * **`VBox_PlayerStats` (VBoxContainer)**
                        * **`Label_PlayerName` (Label)** [Text: "PLAYER (YOU)"]
                        * **`ProgressBar_PlayerHP` (ProgressBar)** [Value: 100, Min: 0, Max: 100]
            * **`Row3_CardHand` (MarginContainer)** *[Anchored Bottom, full width, significant height (e.g., 150px)]*
                * **`Panel_HandBackground` (PanelContainer)** *[Wireframe: Semicircle or Dark rectangle backdrop for the cards]*
                    * **`HBox_Hand` (HBoxContainer)** *[Centered, aligned to bottom]*
                        * **`Card_Slot_1` (Button / PanelContainer)** *[Wireframe: Static placeholder card (e.g., 60x100) or simple button]*
                            * **`VBox_CardDetails` (VBoxContainer)**
                                * **`Label_CardName` (Label)** [Text: "Caught Fish Attack"]
                                * **`Label_CardType` (Label)** [Text: "Basic Damage"]
                                * **`Label_CardCost` (Label)** [Text: "Cost: 1"]

### Interaction Definition:

* **Turn Flow:** Scene loads $\rightarrow$ Initialize Boss turn (set intent) $\rightarrow$ Enable Player turn.
* **Player Input:** Player clicks `Card_Slot_1` (Button). Trigger Card effect logic (e.g., subtract damage from Boss HP, subtract cost from Player AP). Trigger `CombatScene` state machine: 'End Player Turn' $\rightarrow$ 'Execute Boss Turn' $\rightarrow$ 'Check Victory/Loss' $\rightarrow$ Repeat or Exit.
