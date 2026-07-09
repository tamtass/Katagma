# Katagma — Development Diary

---

## 03.01.2026

Started the project properly today. I've had the concept sitting in a notes file for a few months — a top-down 2D roguelike dungeon crawler with a dark, oppressive atmosphere. The name Katagma came from the Greek word for fracture, which felt right given the themes I had in mind. The player is broken, the world is broken, and each run is an attempt to push further through something that keeps resisting.

Set up the Unity project. Using Unity 2022 LTS for stability. Created the basic folder structure — Scripts, Scenes, Prefabs, Textures, Audio. Committed the initial project to git. The first commit is always a bit ceremonial but it matters to have the history clean from the start.

Spent most of the day just thinking about scope. It's easy to plan a roguelike that would take three years to build. I want something that is actually finishable within the diploma timeframe. Decided on the following constraints: procedurally generated dungeon floors, one player character, a handful of enemy types, an item and upgrade system, multiple floors with a boss, and a narrative layer revealed through progression. No branching paths, no meta-progression currencies, no shops. Keep the loop tight.

---

## 07.01.2026

Started on player movement. Using Unity's new Input System rather than the legacy one. It's more work to set up initially but the architecture is cleaner and it means I can add controller support later without rewriting anything.

The player moves using a Rigidbody2D with velocity-based movement rather than transform.position manipulation. This keeps collisions handled by the physics engine properly. Set drag to a high value and manipulate velocity directly each FixedUpdate — this gives a snappy feel without the jitteriness you get from directly setting position.

The camera follows the player using a simple lerp towards the player's position. No Cinemachine for now, might add it later if the camera needs more complexity. The lerp coefficient is exposed in the Inspector so I can tune it during playtesting.

Collision layers set up: Player, Enemy, Wall, Projectile. The interaction matrix in the Physics 2D settings controls what collides with what. Projectiles should not collide with other projectiles, enemies should not push each other out of the way.

---

## 12.01.2026

First pass at the dungeon generation. This is the most technically interesting part of the project so I wanted to get something working early.

The approach is room-based rather than tile-based. The generator places rectangular rooms on a grid, connects them with corridors, and then tiles the result. Each room has a type — start room, combat room, treasure room, boss room, exit room. The generation algorithm works as follows: first, scatter a set of room candidates randomly on the grid, then run a separation step to push overlapping rooms apart, then apply a minimum spanning tree to find the connections, and finally add a small percentage of extra edges back in to create loops. This is a fairly standard approach for dungeon generators and it produces results that feel handcrafted rather than obviously algorithmic.

One problem early on: the boss room sometimes failed to place because I was selecting it from rooms that satisfied too many conditions simultaneously. Switched to an unconditional BFS that always picks the farthest room from the start. Simpler, more reliable.

---

## 18.01.2026

Enemy behaviour system. Went with a basic state machine approach — Idle, Chase, Attack, Dead. The transitions are simple: if the player enters the detection radius, switch to Chase; if within attack range and the cooldown has elapsed, switch to Attack; if health drops to zero, Dead.

Navigation is handled by a custom steering behaviour rather than Unity's NavMesh. For a top-down 2D game with tile-based geometry, NavMesh felt like overkill and it has some annoying edge cases with dynamic obstacles. Instead, enemies use a combination of direct velocity towards the player and an obstacle avoidance layer that reads nearby wall colliders and steers around them. It's not perfect but it produces believable movement.

Enemy base class `Enemy.cs` uses a virtual `Die()` method that derived classes can override. The base implementation handles score, stat tracking, item drops, and destruction. This means adding a new enemy type is just a matter of subclassing and overriding the relevant methods.

Set up the `ScoreValue` field on the Enemy base class. Different enemy types will be worth different amounts.

---

## 25.01.2026

Spent a few days on the room controller system. Each room has a `RoomController` component that tracks whether the room has been cleared. When the player enters a combat room, the exits are sealed. When the last enemy in the room is killed, the exits open and the room is marked cleared.

The sealing mechanic is done by activating invisible collider GameObjects over the doorways. It's simple and reliable — no pathfinding or trigger complexity, just a wall that appears and disappears.

The room controller also needs to categorise rooms — `IsCombatRoom()`, `IsBossRoom()`, `IsTreasureRoom()` — so the right behaviour triggers for each type. Combat rooms and boss rooms call `OnCombatRoomCleared()` on the GameManager when cleared, which increments the rooms cleared stat.

There's a `RoomVisibility` system that ties into the minimap. Rooms start hidden and are revealed when the player enters them. The minimap renders a simplified overhead view of revealed rooms only.

---

## 01.02.2026

Minimap implementation. Created a secondary camera that renders only the minimap layer to a render texture, which is then displayed as a RawImage UI element in the corner of the screen. The minimap camera follows the player but at a fixed orthographic size to give a consistent zoom level.

Room icons are small sprites placed at each room's centre position on the minimap layer. Their colour distinguishes room type — combat, treasure, boss, exit. They activate when the room is first entered.

Took longer than expected because the minimap camera was initially picking up the main scene layer and showing the full dungeon through walls. The fix was layering — main scene objects on one layer, minimap icons on another, and configuring each camera's culling mask accordingly.

---

## 08.02.2026

Added the item system. Items are pickup objects scattered in treasure rooms. The player walks over them and gains a passive upgrade — things like increased attack speed, increased projectile size, or temporary invincibility on room clear.

The item pool in `GameManager` works as a shuffle bag. The full list of item prefabs is loaded into a `List<GameObject>` at the start of each run, and `TakeRandomItem()` removes and returns a random item from the list. This prevents the same item from appearing twice in a single run, which creates more interesting build variety than pure random selection.

The item drop system on enemies uses a chance-based roll. Not all enemies drop items — only the boss room enemy is guaranteed to drop one.

---

## 14.02.2026

First proper playtest session today. Ran through two full floors. A few things became immediately obvious:

The player is too fast relative to the rooms, which makes combat feel like running past enemies rather than fighting them. Reduced movement speed and adjusted the camera lerp to feel less floaty.

Enemy detection radius is too large. Enemies aggro from across the room which doesn't give the player any time to assess the room before being rushed. Reduced to a more reasonable range and added a brief aggro delay before they start moving.

The item drops feel random in a bad way right now. Seeing the same item in two consecutive treasure rooms is frustrating. The shuffle bag system should fix this once more items are in.

Doors opening on room clear feel abrupt. Need a visual or audio cue when the exits unseal.

---

## 21.02.2026

Visual pass on the environment. The original placeholder textures were grey blocks with outline shaders, which was fine for testing but looks bad. Drew a new tileset — stone floor tiles with variation, wall tiles with depth on the top face, corner pieces, doorway frames. The dungeon now reads as an actual dungeon rather than a grid diagram.

The lighting is handled by Unity's 2D lighting system with a global ambient light set very dark and a point light following the player. It gives a torch-like falloff that suits the atmosphere without requiring per-room lighting setup.

Added a subtle camera shake effect on hit and on room clear. It's implemented as a coroutine that applies a random offset to the camera's local position, decaying over the shake duration. Small effect but it makes hits feel much more impactful.

---

## 01.03.2026

Health and combat refinements. The player has a health pool with a visible health bar in the HUD. The health bar uses a custom shader that fills from left to right based on current health percentage. There's a secondary bar that lags behind the primary one and catches up slowly — the classic "damage ghost" effect that communicates how much health was just lost.

The player's attack is a projectile — a bolt that travels in the direction of the mouse cursor. The projectile uses a Rigidbody2D with a preset velocity and destroys itself on contact with anything in the Wall or Enemy layer. Each bolt can only hit one enemy.

There was an early bug where a bolt would sometimes hit the same enemy multiple times in a single frame due to multiple collision callbacks firing. Fixed by flagging the projectile as "hit" on first contact and ignoring subsequent collisions.

Added an invincibility window after taking damage. The player flashes for a short period and cannot take additional damage. This is standard for the genre and makes the combat feel fairer.

---

## 09.03.2026

Floor progression. The exit room contains a portal that the player activates to advance to the next floor. `GameManager.AdvanceFloor()` increments the floor counter and generates a new dungeon layout.

The game currently has three floors. The final floor contains a boss room with a tougher enemy variant. Defeating the boss and leaving the boss room triggers the win condition.

The transition between floors uses the `ShowFloor` sequence: fade to black, display the floor title ("FLOOR 2" etc.) with a scale-in animation, hold briefly, fade the title out, then fade back in to the new floor. This gives the player a moment of anticipation between floors.

One complication: the floor generator needs to clear all GameObjects from the previous floor before generating the new one. This includes enemies, projectiles, pickups, and room controller components. Wrote a `ClearFloor()` method that iterates over all registered objects and destroys them. The player GameObject persists across floors and is repositioned to the new start room.

---

## 16.03.2026

Worked on the upgrade system. When the player clears a combat room, there's a chance an item pedestal spawns in the centre. The player can pick up the item for a permanent upgrade for the rest of that run.

The upgrades are ScriptableObjects — each defines a name, description, icon, and effect. The effect is implemented as an interface `IUpgradeEffect` with an `Apply(PlayerMovement player)` method. This makes adding new upgrades clean — create a new ScriptableObject, implement the interface, done.

Current upgrades: faster fire rate, larger projectile, brief speed boost on kill, health regeneration per room cleared, extra projectile that fires at a slight angle. The build variety even with just five upgrades is noticeable.

---

## 23.03.2026

Added statistics tracking. Throughout the session, the GameManager accumulates:
- Current score (with a one-point-per-second passive decay to punish slow play)
- Elapsed time
- Enemies killed
- Combat rooms cleared
- Floors cleared

The score decay mechanic was a late addition that immediately made the game more interesting. It creates a constant low-level pressure to keep moving rather than sitting safely in cleared rooms.

`OnEnemyKilled()` and `OnCombatRoomCleared()` are public methods on `GameManager` called by `Enemy.Die()` and `RoomController.ClearRoom()` respectively. It took a refactor to wire these in cleanly — the original system had the stats scattered across multiple scripts with no single source of truth.

---

## 30.03.2026

Game over screen implementation. When the player dies, the game should show a stats summary — score, time, enemies killed, rooms cleared, floors cleared. This screen also doubles as the win screen when all floors are completed.

The `GameOverScreen` component has separate GameObjects for the win and lose states (different title text and background images). `Show(bool isWin)` activates the correct set and fills in the stat text fields.

First bug: the GameOverScreen was permanently invisible. Root cause — `Awake()` was calling `gameObject.SetActive(false)`, and when `Show()` called `SetActive(true)`, Unity ran `Awake` again (it had never actually initialised), which immediately called `SetActive(false)` again. A completely silent loop. Fixed by removing the `SetActive(false)` from `Awake` and initialising state purely through `GameManager.Awake()` instead.

---

## 06.04.2026

The death sequence was the most technically involved UI problem so far. The design: player dies, screen fades to black, the word "FRACTURED" fades in and holds, then fades out, then the overlay lifts to reveal the already-visible game over screen underneath. The game world should not be visible at any point between death and the stats reveal.

Getting this timing right required several iterations. The first attempt had the FRACTURED text and the game over screen both trying to animate simultaneously, which looked wrong. The second attempt had a gap where the black overlay faded out too early before the game over screen was active, briefly showing the game world.

The final solution: the overlay fades to black first, then the `onBlack` callback activates the game over screen while the overlay is still fully opaque. FRACTURED then animates in and out on top of the overlay. Only once FRACTURED has finished fading out does the overlay itself fade away, revealing the stat screen.

One more bug during this phase: enemies were continuing to attack during the death animation. The issue was that `Time.timeScale = 0f` was being called inside `ShowGameOver()`, which only ran after the full FRACTURED animation completed. Fixed by moving the timeScale set to the very first line of the player's `Die()` method so everything freezes immediately on death.

---

## 13.04.2026

The transition system had grown organically to the point of being fragile. There were at least three different places setting CanvasGroup alpha values, multiple coroutines that could conflict with each other, and screens being shown and hidden inconsistently — some used `SetActive`, some used alpha, some used a combination.

Rewrote the whole system. The design principle is simple: one black CanvasGroup overlay handles all fading. All screens use `SetActive` exclusively — nothing is shown or hidden by tweening alpha on screen-level CanvasGroups. The sequence for any transition is always: fade overlay to black, run the swap callback, animate any text elements if applicable, fade overlay back to transparent.

The `TransitionScreen` singleton is a `DontDestroyOnLoad` object that exposes three public methods beyond the basic `Transition`:
- `FadeFromBlack` — used only once on startup to fade in from the initial black screen
- `ShowDeath` — handles the full FRACTURED death sequence
- `ShowFloor` — handles the floor title sequence with the scale-in animation

The `Run()` method stops any current coroutine before starting the new one, preventing overlapping sequences.

---

## 20.04.2026

Story progression screen. The narrative idea behind Katagma is that there is a backstory told through fragmented images — five pieces of artwork that are revealed one at a time as the player wins runs. On first run everything is blacked out. Win the game, and one random image fades from black to white. Win again, another one unlocks.

The tint approach (image Color going from black to white) was the simplest way to implement this without needing separate textures or masking. The unlock state is persisted between sessions using PlayerPrefs with a comma-separated index string. On `Show()`, previously unlocked images are immediately set to white, and a coroutine then animates one new image if any remain locked.

There was a persistence bug: the continue button on the story screen wasn't triggering the return to main menu correctly. The screen was being deactivated before the fade transition started, which meant there was no screen to fade out — the transition was running against nothing. The fix was the same as the game over screen issue: move the `SetActive(false)` call inside the `onBlack` callback so it executes while the overlay is opaque.

---

## 27.04.2026

Options menu. Added a settings panel accessible from the main menu. Currently contains a mute toggle. The `IsGameMuted` property on `GameManager` maps directly to `AudioListener.volume`, which is the bluntest-instrument approach but it works globally across all audio sources without needing to track individual AudioSource components.

Escape handling in `InputManager` was extended to cover the options menu: if options is open and escape is pressed, call `CloseOptions()`. This check runs before the in-game pause check, so there's no ambiguity.

The options menu uses the same transition system as everything else — fade to black, swap, fade from black. The transition durations for the options menu are shorter than the floor transitions because it's a menu navigation action rather than a dramatic game moment.

---

## 04.05.2026

Delete save functionality. In the options menu there's a delete save button. Pressing it opens a confirmation popup with two buttons — yes and no. The yes button deletes all PlayerPrefs, destroys the `GameManager` and `TransitionScreen` persistent singletons, and reloads the scene. The destruction step is critical: without it, the singletons survive the scene reload and hold stale references to destroyed GameObjects, causing null reference errors on the freshly loaded scene.

The `DeleteSavePopup` is a simple three-method script — `Show()`, `OnYesClicked()`, `OnNoClicked()`. No dependencies on the transition system because the reload will handle the visual reset.

---

## 11.05.2026

Escape key handling was formalised across all menus. The `InputManager` now handles escape in the following priority order: options menu → leaderboard → in-game pause. This means wherever the player is, escape always does the sensible thing. The logic is just a chain of active-state checks on the relevant UI GameObjects.

Spent time on general polish — sound effects for hits, pickups, room clear, and floor transitions. The audio design is minimal but present. Silence in a game this atmospheric would feel wrong.

---

## 18.05.2026

Started planning the leaderboard feature. Wanted players to be able to submit their scores and see how they compare globally. Evaluated several backend options.

Firebase Realtime Database was the first consideration but the query limitations made it awkward for a sorted leaderboard. Firestore was the better fit — it supports `orderBy` and `limit` at the query level, so the server does the sorting rather than the client. That matters when there could eventually be thousands of entries.

The Firebase Unity SDK was ruled out almost immediately. It's designed for mobile (Android/iOS) and has a messy setup on desktop builds. For a Windows desktop game, the SDK would require more configuration than the feature is worth. The alternative is Firestore's REST API, which is just HTTP requests. Unity's `UnityWebRequest` handles those natively with no additional packages.

The Firebase project was set up through the Firebase Console. The app was registered as a Web app (not Android or iOS) to get the API key and project ID. Those two values are all the REST API needs.

---

## 25.05.2026

Implementing the leaderboard backend. The `LeaderboardManager` is a self-creating singleton — if no instance exists when accessed, it creates its own GameObject and marks it `DontDestroyOnLoad`. This means it never needs to be placed in the scene manually.

Two operations: submit and fetch.

Submit sends a POST request to the Firestore documents endpoint with a JSON body containing the player's name, score, floors cleared, and time. Firestore's REST API requires integer values to be encoded as strings in the JSON body — this is a documented quirk of the REST representation of int64 values. The body is built manually as a formatted string rather than using a JSON serialisation library, which keeps the code dependency-free.

Fetch sends a POST request to the `runQuery` endpoint with a structured query body: from the `leaderboard` collection, ordered by score descending, limit 100. The response is a JSON array of document objects.

Parsing the response required a workaround for `JsonUtility`. Unity's built-in JSON utility cannot deserialise JSON arrays directly — it expects a root object. The fix is to wrap the response array in `{"items": [...]}` before passing it to `JsonUtility.FromJson`. The deserialisation classes mirror the Firestore field value type structure (`stringValue`, `integerValue`, `doubleValue`).

---

## 01.06.2026

Leaderboard UI. The leaderboard screen opens from the main menu via the standard transition. When the screen becomes active (`OnEnable`), it triggers a fetch automatically. While the fetch is in progress, a status text shows "Loading…". On success, row prefabs are instantiated into the scroll view content for each entry. On failure or empty result, the status text shows the appropriate message.

The `LeaderboardRowUI` script on each row prefab exposes three text fields — rank, name, score. The additional data (floors cleared and time) is stored in Firestore and in the `LeaderboardEntry` object but not currently displayed, in case more columns are added later.

A refresh button on the leaderboard screen calls `Refresh()` directly, which repeats the clear-and-fetch cycle. This is the same logic as `OnEnable` so there's no duplication.

---

## 08.06.2026

Scroll view problems. Two separate issues.

The first: the scroll rect content was jumping back to position zero after scrolling. The cause was the `ContentSizeFitter` component on the Content object recalculating the content height asynchronously. When rows are instantiated and the layout hasn't rebuilt yet, the scroll rect sees an incorrect (too small) content height and clamps the scroll position accordingly. Adding `LayoutRebuilder.ForceRebuildLayoutImmediate` immediately after row instantiation forces the layout calculation to happen synchronously before the scroll rect reads the size.

The second: rows were stacking directly on top of each other after adding a `VerticalLayoutGroup` to the Content object. The layout group had `Control Child Size → Height` checked, which overrides each child's height with the layout group's own calculation. Since the row prefabs had no `LayoutElement` component specifying a preferred height, the layout group resolved their height to zero. Unchecking `Control Child Size → Height` restores the rows' own RectTransform height values and the layout group only handles vertical stacking.

---

## 15.06.2026

Submit score popup. Added a `SubmitScorePopup` script that appears from the game over screen. The popup has a name input field, a submit button, a cancel button, and a status text element. The player's last-used name is stored in PlayerPrefs and pre-filled on open so they don't have to type it every time.

The submit button disables itself (and the input field) while the request is in flight, preventing double-submits. On success, the status text briefly shows a confirmation message and then the popup closes automatically after a short delay. On failure, the status text shows an error and the controls re-enable for retry.

The score, floors cleared, and time values are read from `GameManager.Instance` at the moment of submission, so the popup doesn't need to receive them as parameters.

---

## 20.06.2026

Firestore security rules were configured in the Firebase Console. The rules allow unrestricted reads (anyone can view the leaderboard) and validated writes. Write validation checks that the `name` field is a string under 20 characters and that `score` is a non-negative integer. This prevents the most obvious trivial abuse — blank names, negative scores, garbage data types — without requiring user authentication.

The rules do not prevent score spoofing entirely. Without server-side validation, a determined player could submit an arbitrarily high score. For a diploma project, this tradeoff is acceptable. A full anti-cheat solution would require a server that validates the score against a signed session, which is out of scope.

---

## 24.06.2026

Final pass and documentation. Wrote the dev diary as a continuous record of the development process. The diary covers decisions made, bugs encountered, solutions found, and iterations on features that didn't work the first time.

Looking back at the project as a whole: the codebase is cleaner than I expected it to be at this stage. The transition system was the biggest architectural headache — it went through three designs before settling into something stable. The Firebase integration ended up being simpler than anticipated once the SDK was ruled out in favour of the REST API.

The systems that took the longest were not the ones I expected. The dungeon generator was conceptually the most complex part but came together relatively smoothly. The UI transition timing and the game over screen activation bug were the most time-consuming to debug despite being small in scope. This is a pattern that comes up in game development often: the technically interesting problems are not always the hardest ones to solve.

The game is playable, has a start and an end, tells a small story through the image progression system, and has a live leaderboard. That was the plan from the beginning and it's what exists now.

---

## Algorithm and Implementation Notes

The following entries go into greater technical depth on the core algorithms used throughout the project. They were written partly during development and partly in retrospect, as a record of the reasoning behind implementation choices.

---

### 13.01.2026 — Dungeon Layout: Random Walk Expansion

The floor generation algorithm works in two distinct phases: layout generation and type assignment. Understanding why these are separate is important. The layout phase only decides which grid cells will contain rooms and how many. The type assignment phase then decides what role each room plays. Keeping them separate meant the layout algorithm could be tuned purely for topology quality without worrying about game logic.

The layout algorithm is a constrained random walk expansion. It maintains a growing set of occupied grid positions starting at the origin. On each iteration it needs to pick a source cell to expand from, then a direction to expand into. The key constraint is `maxNewRoomNeighbors`: a candidate position is rejected if it already has more than this many occupied neighbours. This single parameter controls the feel of the generated map. Setting it to 1 produces long, branching corridors with few loops. Setting it to 2 allows some clustering. Setting it higher produces dense blobs. The value 1 was chosen because it gives maps that feel like dungeon corridors rather than open areas.

The source cell selection uses a soft-minimum heuristic: cells are ranked by `CountNeighbors + Random.value * 0.49f` and the cell with the lowest score is chosen. The `* 0.49f` scaling on the random noise ensures the noise never actually overrides a real difference in neighbour count — a cell with 1 neighbour will always rank below a cell with 2 neighbours even at extreme noise values, because the noise cannot add more than 0.49. This keeps the expansion preferring frontier cells (those with fewer connections) while introducing enough randomness that the layouts are not deterministic.

`CountNeighbors` itself is a simple lambda over the four cardinal directions. For a given position, it counts how many of the four adjacent cells are already in the occupied set. This is an O(1) operation because the positions are stored in a HashSet, giving O(1) membership tests.

The safety limit of `targetRoomCount * 500` prevents infinite loops in the unlikely case that valid expansion positions run out before the target count is reached. In practice this limit is never hit for reasonable parameters.

---

### 15.01.2026 — Dungeon Layout: BFS Distance and Room Type Assignment

Once the layout positions exist, they need to be assigned types: starting room, normal room, item room, boss room. The assignment is driven by graph distance, specifically breadth-first search distances from the origin.

BFS was chosen over Dijkstra because all edges in the room graph have equal weight — moving from one room to an adjacent room always costs 1. BFS gives exact shortest-path distances in O(V + E) time, which for a graph of 10 rooms is essentially instantaneous. The BFS function takes an origin position and the set of occupied positions, and returns a dictionary mapping every reachable position to its distance from the origin.

The boss room assignment is the most important: it should always be the room that requires the most travel to reach, forcing the player to explore rather than running directly to the exit. This is done by running BFS from the origin and taking the position with the maximum distance. Crucially, this is done unconditionally — any position can be the boss room, not just dead ends. An earlier version required the boss room to be a dead end (exactly one neighbour), but this caused generation to occasionally fail when the farthest position happened to be a junction. Removing that constraint fixed the issue entirely.

Item rooms are assigned next. A second BFS runs from the boss room position. Item rooms are the positions farthest from the boss room (excluding already-assigned positions), up to `maxItemRooms`. The reasoning: item rooms contain power upgrades, and they should reward exploration. Placing them far from the boss room means a player who explores thoroughly gets stronger before the final fight. It is also unlikely that item rooms will cluster near the start, which would let a careful player always find both items before even the first combat room.

Dead end detection — positions with exactly one neighbour — was used in an earlier version of the type system but is now only computed as metadata, not used for assignment. The shift towards purely distance-based assignment was made because dead ends are not guaranteed to be far from the start, and distance is a better proxy for "this room should be a reward."

---

### 19.01.2026 — Enemy Movement: Direction Vector and Atan2 Rotation

The enemy movement is simple by design. In each `FixedUpdate`, the enemy computes the normalised vector from its current position to the player's position and assigns it directly to the Rigidbody2D velocity, scaled by `moveSpeed`. This is the most direct possible implementation of chase behaviour. It has a known weakness — enemies will path directly towards the player and pile up on walls or each other — but for a dungeon crawler where rooms are small and the player is the agile party, this produces engaging rather than broken combat.

The normalisation step is important. Without it, enemies farther from the player would move faster because the vector magnitude scales with distance. Normalising first, then multiplying by `moveSpeed`, gives constant speed regardless of distance.

The enemy sprite rotates to face the player using `Mathf.Atan2`. `Atan2(y, x)` returns the angle in radians of a 2D vector. The inputs are the y and x components of the normalised direction vector to the player. The result is converted from radians to degrees and applied as a Z-axis rotation via `Quaternion.Euler(0, 0, angle)`. `Atan2` handles all quadrants correctly, including edge cases like the player being directly above or below (where a simple `Atan` would be undefined or would require special handling).

Two timers gate the movement: `spawnStunTimer` and `knockbackTimer`. The enemy does not move while either timer is above zero. The spawn stun gives the player a brief moment to react when entering a room. The knockback timer freezes the enemy briefly after being hit, providing visual and mechanical feedback that the hit landed.

---

### 22.01.2026 — Enemy Spawning: Budget System and Rejection Sampling

The enemy spawning algorithm in `RoomController` uses a budget-based approach rather than a fixed count. Each room is assigned a random spawn budget within a configurable range. Each enemy type has a `spawnWeight` value. The algorithm repeatedly picks a random enemy from the pool and subtracts its weight from the remaining budget, then removes any enemies whose weight exceeds the remaining budget. This continues until no eligible enemies remain.

The result is that rooms have variable numbers of enemies that always fit within the budget envelope — a room with a large budget might have two heavy enemies or five light ones, but not two heavy enemies plus five light ones. This is analogous to a knapsack packing but solved greedily with random selection rather than optimally, which is more appropriate for a game where unpredictability is a feature.

Spawn position uses rejection sampling. A candidate position is chosen uniformly at random within a rectangular area centred on the room. If the candidate is closer than `minDistanceFromEntry` to the point where the player will enter the room, it is rejected and a new candidate is drawn. A maximum of 10 attempts prevents an infinite loop in edge cases. The entry point distance check prevents enemies from spawning directly on top of the player, which would be unfair. After 10 failed attempts the position is accepted anyway — the situation where every random point is within the minimum distance is geometrically very unlikely given the room dimensions.

---

### 05.02.2026 — Player Movement: Acceleration Model

Player movement uses an acceleration and deceleration model rather than direct velocity assignment. In `FixedUpdate`, the current velocity is moved towards a target velocity using `Vector2.MoveTowards`. The target velocity is `moveInput * movementSpeed` when input is held, and `Vector2.zero` when no input is detected.

`Vector2.MoveTowards` moves a value towards a target by at most a given step per call. The step is `acceleration * Time.fixedDeltaTime` when moving and `deceleration * Time.fixedDeltaTime` when stopping. This produces a non-instant response that makes the character feel weighty and physical rather than teleporting between velocity states. The acceleration and deceleration values are different — stopping is faster than accelerating — which makes the character feel responsive to the player's intention to stop while still having momentum when changing directions.

The movement input vector is normalised before use. Without normalisation, diagonal movement (holding W and D simultaneously) would produce a vector of magnitude √2, making diagonal movement 41% faster than cardinal movement. Normalisation ensures uniform speed in all directions.

Knockback interrupts movement for a fixed duration. While `knockbackTimer > 0`, the velocity assignment is skipped entirely, allowing the impulse force from `TakeDamage` to play out unimpeded. The impulse is applied using `ForceMode2D.Impulse`, which adds a velocity change in a single physics step rather than over time.

---

### 08.02.2026 — Player Facing: Axis-Dominant Sprite Selection

The player sprite selection uses an axis-dominant check to determine which of the four directional sprites to display. The vector from the player's world position to the mouse cursor's world position is computed by converting the mouse screen position to world coordinates using `Camera.main.ScreenToWorldPoint`.

The direction vector is decomposed into its x and y components. If the absolute value of x is greater than or equal to the absolute value of y, the mouse is more to the left or right than it is above or below, so the horizontal sprite is used. Otherwise the vertical sprite is used. Within each axis, the sign of the component determines left/right or up/down.

The angle is also stored as a floating-point value in degrees for use by the attack system. This avoids recalculating the direction every frame in `HandleAttack`.

A dead zone check (`sqrMagnitude < 0.001f`) prevents the facing from updating when the cursor is extremely close to the player — this prevents flickering when the cursor happens to sit exactly on the player position.

---

### 11.02.2026 — Attack System: Cone Hit Detection with 2D Cross Product

The attack is the most technically interesting system in the game. Visually it fires a fan of lightning bolts in a cone in front of the player. Mechanically, it needs to determine which enemies are hit and by how many bolts, because multiple bolts hitting a single enemy deal proportionally more damage.

The hit detection uses a two-step process. First, `Physics2D.OverlapCircleAll` finds all colliders within `attackRange` distance of the player. This is a broad phase check that uses Unity's spatial partitioning to avoid checking every enemy in the scene. Only objects within the attack range circle are candidates.

For each candidate enemy, a cone membership check is performed. The angle between the player's facing direction and the direction to the enemy is computed using `Vector2.Angle`. If this angle exceeds `coneHalfAngle`, the enemy is outside the attack cone and is skipped.

For enemies within the cone, the number of bolts that hit them is computed using the 2D cross product. Each bolt travels along a specific direction vector, computed from its spread angle within the cone. The 2D cross product `|P × D|` gives the perpendicular distance from a point P to a ray in direction D. Specifically, for a direction vector `boltDir` and a vector `dirToTarget` pointing from the player to the enemy, the perpendicular distance is `|dirToTarget.x * boltDir.y - dirToTarget.y * boltDir.x|`. If this perpendicular distance is less than or equal to the enemy's collision radius, the bolt is considered to pass through the enemy.

The enemy's collision radius is approximated as the maximum of the bounding box's x and y extents. This overestimates the radius for non-circular enemies but errs on the side of generosity to the player, which feels better than under-counting.

After all bolts are tested, the total hit count for that enemy is passed to `TakeDamage`, multiplied by the base damage value. An enemy hit by two bolts takes twice the damage of one hit by a single bolt. A minimum of 1 hit is enforced — if the cone check passed but no individual bolt cross-product test succeeds, the enemy takes one bolt's worth of damage. This prevents the corner case where the enemy is at the edge of the cone but the perpendicular test misses due to discrete bolt spacing.

---

### 05.03.2026 — Lightning Bolt Rendering: Midpoint Displacement

The lightning bolts are rendered using Unity's `LineRenderer` component with a midpoint displacement algorithm to generate the jagged, branching appearance.

Each bolt is a `LineRenderer` with `segments + 1` points. The first and last points are fixed: the player's position and the endpoint at `origin + direction * range`. The intermediate points are placed along the straight line between origin and endpoint and then displaced perpendicular to the bolt direction.

The perpendicular vector is computed as `(-dir.y, dir.x, 0)` — the 90-degree rotation of the bolt direction in 2D. For each intermediate point at normalised position `t` along the bolt, the maximum displacement is scaled by `1 - |t - 0.5| * 2`. This is a tent function that peaks at the midpoint (t = 0.5) and falls to zero at the endpoints (t = 0 and t = 1). The result is that intermediate points can be displaced more if they are near the middle of the bolt and less if they are near the endpoints, which forces the bolt to start and end at the correct positions while having maximum visual chaos in the middle.

The actual displacement for each point is `Random.Range(-maxDisplace, maxDisplace)` applied along the perpendicular vector. Because the random values are drawn independently for each segment, the bolt does not look like a smooth sine curve — it has the discrete, angular zigzag appearance of a real lightning bolt.

The bolts regenerate at a rate of `flickerRate` seconds, redrawing with new random displacements each time. This flickering is what gives lightning its characteristic animated quality. The regeneration is gated by `durationTimer`, which shuts off all renderers when the attack animation is complete.

When the player gains additional projectiles, the bolt count changes. The `Play` method detects a count change and calls `RebuildBolts`, which destroys the existing `LineRenderer` GameObjects and creates new ones. This is an infrequent operation (only when projectile count changes) and the allocation cost is acceptable.

---

### 14.03.2026 — Room Transition: SmoothStep Camera Pan

When the player moves through a door, the camera pans from the centre of the current room to the centre of the adjacent room. The pan uses linear interpolation with a SmoothStep easing function.

SmoothStep takes a normalised time value `t` in [0, 1] and returns `3t² - 2t³`. This curve starts at 0, ends at 1, and has zero derivative at both endpoints. In practical terms, the camera starts slow, accelerates through the middle of the pan, and decelerates to a stop at the destination. This is more visually comfortable than a linear pan, which would feel mechanical, or a raw ease-out, which would feel like the camera is skidding to a halt.

The elapsed time is accumulated using `Time.deltaTime` in a coroutine, divided by `transitionDuration` to get the normalised `t`. The SmoothStep result is passed to `Vector3.Lerp` to compute the actual camera position each frame.

During the transition, the player's `canMove` flag is set to false, preventing movement input from being processed. The player is teleported to the entry spawn point of the new room only after the camera pan completes, so the player is never seen in an inconsistent position. The new room is activated before the pan begins so it is visible as the camera arrives, rather than popping into existence at the end.

The opposite spawn point lookup uses a direction inversion: if the player exits through the top door, they enter the new room from the bottom, and the spawn point is the bottom spawn point of the new room. This lookup is a simple switch expression on the direction enum.

---

### 22.03.2026 — Score Decay: Linear Penalty Timer

The score penalty is implemented as an accumulating timer in `GameManager.Update`. Each second of real game time, one point is deducted from the player's score, clamped at zero. The accumulation uses a dedicated `_penaltyTimer` float rather than checking `Mathf.FloorToInt(ElapsedTime)` against a counter, because the floor-based approach requires storing the last tick value and comparing it each frame. The timer approach is cleaner: increment by `deltaTime`, subtract 1f when it reaches 1f.

This runs only when `IsGameRunning && !IsPaused && !IsPlayerDead`. Time scale is set to zero on death and pause, so `Time.deltaTime` becomes zero and the timer stops naturally without additional flag checks in those states.

---

### 28.03.2026 — Invulnerability and Flicker: Discrete Sine Approximation

After taking damage, the player enters an invulnerability window during which they cannot take further damage. The invulnerability timer counts down from `invulnerabilityDuration`. During this time the sprite flickers to signal the invulnerable state to the player.

The flicker is implemented using a discrete square wave. `Mathf.FloorToInt(invulnerabilityTimer * flickerFrequency)` converts the continuous timer into a sequence of integers. Taking this value modulo 2 gives either 0 or 1, alternating as time passes. This drives the sprite between full opacity and a very low alpha (0.2). The `flickerFrequency` parameter controls how many on-off cycles occur per second of invulnerability time.

The same technique appears in the pickup freeze flicker in `PickupFreezeCoroutine`, where it drives a black-and-white alternation using a different frequency value. Using the same pattern in both places means both visual effects have a consistent implementation.

---

### 10.04.2026 — Item Pool: The Shuffle Bag Pattern

The item pool uses a shuffle bag rather than a uniform random selection. The full list of item prefabs is loaded into a `List<GameObject>` at the start of each run. `TakeRandomItem()` selects a random index from this list, removes that element, and returns it. Subsequent calls draw from the shrinking list.

The consequence is that items never repeat within a single run. In a game with five items and three item rooms, the player will see three distinct items from the five available, never the same item twice. This is more interesting than uniform random selection, which can produce runs where the same item appears two or three times.

The pattern is called a shuffle bag because it is conceptually equivalent to putting all items in a bag, shaking it, and drawing one out without replacement. The implementation using index-based removal from a List is O(n) for the removal step, but for a list of five to ten items this is negligible.

---

### 02.05.2026 — Per-Room Stat Upgrade: Multiplicative Scaling

When the player clears a combat room, `OnRoomCleared()` is called on `PlayerMovement`. This selects one of five stats at random — movement speed, attack speed, damage, attack range, or projectile count — and applies a 10% increase relative to the current value of that stat. All upgrades except projectile count are multiplicative and accumulative: a movement speed that starts at 5 and receives three upgrades will become `5 × 1.1 × 1.1 × 1.1 ≈ 6.655`.

Projectile count is the exception — it increases by a flat 1 per upgrade because it is a discrete integer and a percentage increase would need to be floored, producing inconsistent results.

The 10% rate was chosen by playtesting. Larger values (25%) made the player feel overpowered within two rooms. Smaller values (5%) made the upgrades barely noticeable. At 10%, the power curve over a full floor is steep enough to feel rewarding but not so steep that early floor clears trivialise the boss.

The upgrade flash — the UI element that briefly shows which stat was upgraded — uses a fixed `WaitForSeconds` value cached as a static field. Caching `WaitForSeconds` objects prevents a small garbage allocation each time the coroutine is triggered, which matters less in a menu-driven context but is good practice.

---

### 17.05.2026 — Firestore REST API: JSON Serialisation Without a Library

The Firestore REST API uses a specific JSON schema for representing typed values. Every field is an object with a single key indicating the type — `stringValue`, `integerValue`, `doubleValue`, etc. Integer values are represented as JSON strings (not numbers) because Firestore's native integer type is 64-bit and JSON numbers cannot represent all int64 values without precision loss.

Building the request body without a JSON library means constructing the string manually. The format for a submit request is a fixed template with the player name, score, floors cleared, and time substituted in. The player name undergoes a minimal escape step — backslashes and double quotes are escaped — to prevent the name string from breaking the JSON structure if it contains those characters. Newlines and carriage returns are stripped rather than escaped, since multiline names are not meaningful.

Parsing the response without a library required the wrapper trick. `JsonUtility.FromJson` does not accept a root-level JSON array, so the response string is prefixed with `{"items":` and suffixed with `}` before being passed to the deserialiser. The deserialiser then populates a class hierarchy that mirrors the Firestore nested object structure: a `_QueryWrapper` containing an array of `_QueryItem`, each containing a `_FS_Doc`, each containing a `_FS_Fields` with named properties for each Firestore field type. These are private nested classes inside `LeaderboardManager` so they do not pollute the global namespace.

The `[Serializable]` attribute is required on each nested class for `JsonUtility` to process it. Fields in non-serializable classes are silently skipped, which produces empty results with no error — the kind of silent failure that took some time to verify was not the issue during initial testing.

---

## 02.07.2026

Redesigned the enemy visual behaviour today. The original implementation rotated the enemy sprite to face the player on every physics tick using Atan2. That looked wrong once the new artwork came in — the enemy sprites are now designed as forward-facing blobs and rotation would just tilt them sideways in a way that reads as broken rather than menacing. Frozen rotation also makes the movement feel more alien, which fits the aesthetic.

Removed the Atan2 rotation from `FixedUpdate` entirely. The Rigidbody2D already had `freezeRotation = true`; the code was overriding the transform rotation directly each frame, bypassing that. With the rotation line gone, the enemy just slides toward the player with no visible turning.

Added two animation systems driven by inspector-exposed frequencies. The first is a two-frame sprite flipper: a `frames` array takes the two sprites, and a `frameFrequency` float controls how many flips per second occur. A simple accumulating timer in `Update` compares against `1f / frameFrequency` and swaps `spriteRenderer.sprite` when the threshold is crossed. The timer resets to zero rather than subtracting the interval to avoid drift accumulation over long runs.

The second is a scale pulse: `transform.localScale` is set each frame to `Vector3.one * (1 + amplitude * sin(time * frequency * 2π))`. The `scaleFrequency` and `scaleAmplitude` fields are both exposed in the inspector. Amplitude is clamped to 0.5 maximum via `[Range]` to prevent the enemy from growing so large it becomes misleading about its collision bounds.

Both systems activate only after the spawn animation completes. `SpawnScale` sets a `spawnDone` flag at the end of its coroutine; `Update` returns early while the flag is false. This keeps the spawn scale-up clean without interference from the pulse.

The `[Min(0.1f)]` attribute on the frequency fields prevents division by zero in the frame flip interval calculation.

---

## 02.07.2026 — Teleporter Enemy

Added a second enemy type: `TeleporterEnemy`. It extends `Enemy` but overrides the movement model entirely — `moveSpeed` is forced to zero in its `Start()` so the base `FixedUpdate` still runs (handling knockback and damage timers correctly) but never applies movement velocity.

The behaviour runs as a single infinite coroutine: wait for spawn animation → shoot → pause → teleport → pause → repeat. The shoot step flashes the sprite orange for a configurable windup duration as a telegraph before firing. After the windup, it instantiates an `EnemyProjectile` prefab and calls `Launch(direction, damage, gameObject)` on it, passing itself as the spawner reference so the projectile's trigger ignores its own creator.

`EnemyProjectile` is a new component. It takes a `Vector2` direction and damage value from the caller, sets `rb.linearVelocity` in `Launch()`, and destroys itself on hitting the player or after a lifetime. Gravity is zeroed in `Awake()`. The collider is a trigger; only the Player tag triggers damage.

The teleport step scales the enemy down to zero over `teleportHalfTime` seconds using SmoothStep, snaps the transform to the new position, then scales back up. During this animation, a `suppressScalePulse` flag (added to the base `Enemy` class) prevents the normal scale-pulse from fighting the animation. After scale-in completes, the flag is cleared.

For picking a teleport destination, the code queries the parent `RoomController` for its `spawnAreaHalfExtents` and center, then samples random points within that rectangle. A candidate is accepted if it is at least `minPlayerDistance` units from the player. After `maxTeleportAttempts` failures (e.g., the room is tiny and the player is in the centre), it falls back to the farthest of eight random samples to guarantee a result without an infinite loop.

---

## 02.07.2026 — Boss Health Bar

Added a `BossHealthBar` component for the boss room UI. The existing `HealthBar` is tightly coupled to `PlayerMovement` so a separate script was written rather than making the base class generic.

`BossHealthBar` mirrors the player bar's sliding fill behaviour: `displayedHealth` moves toward `boss.health` each frame at `slideSpeed` units per second, so damage is reflected with a smooth drain rather than a hard cut. The fill width is calculated the same way — `offsetMax.x` is set based on the fill ratio multiplied by the fillable area (base width minus the left and right border insets read at `Awake`).

The boss reference is found automatically via `FindFirstObjectByType<Enemy>()` in `Update`. This works because the boss room only has one enemy and other rooms are inactive when the boss room is being played. The initial health is cached as `maxHealth` at the moment the boss is first detected, which covers the brief spawn delay before the boss GameObject exists.

When the boss dies, `Destroy` removes the GameObject, making the cached reference go null. On the next frame, the bar detects `initialized && boss == null` and switches into a drain-to-zero mode: it continues sliding `displayedHealth` down to 0, then calls `gameObject.SetActive(false)` to hide the bar. This gives the same visual closure as the health drain on a hit rather than the bar snapping away immediately.

(Later revised: the auto-discovery via `FindFirstObjectByType` was picking up whichever enemy existed first, not necessarily the boss, so `RoomController.SpawnBoss` now hands the boss reference directly to the bar via `SetBoss`. The manual RectTransform fill maths was also replaced with `Image.fillAmount` on a Filled image, which sidesteps the layout-timing problem where border insets were read before the Canvas had finished its first layout pass. The same `fillAmount` approach was then applied to the player bar, and its slide switched to `Time.unscaledDeltaTime` so it still animates down to zero while the death screen has frozen `Time.timeScale`.)

---

## 02.07.2026 — First Boss: The Amygdala ("The Fear")

Implemented the first floor boss. The design brief was a large, near-stationary boss with a two-phase, health-gated fight, and the key constraint I set myself was to reuse the existing systems rather than build parallel machinery. `AmygdalaBoss` therefore derives from the `Enemy` base class. This means score attribution, the damage flash, death handling, item drops, the boss health bar wiring in `RoomController.SpawnBoss`, and the room-clear-to-portal sequence all work through the paths that already exist — the boss is just another entry in the room's `spawnedEnemies` list, so when it is destroyed the room's `Update` sees the list empty and reveals the exit door exactly as before.

It does not chase. Rather than rewrite the base movement, `Start` forces `moveSpeed = 0` before calling `base.Start()`, so the inherited `FixedUpdate` still runs its knockback and timer bookkeeping but resolves to zero velocity. The same trick was used for the teleporter enemy.

The two phases are modelled explicitly with a private `Phase` enum rather than scattering HP-ratio checks through `Update`. Phase state is read by the attack loops to decide cadence. Each attack is its own coroutine loop started once, running on an independent `WaitForSeconds` timer, which is what makes the attacks interleave naturally without a central scheduler:

- **Radial burst** — fires a full ring of evenly spaced projectiles. Even spacing is what creates the safe gaps between bolts. The interval is read from the phase each iteration, so the same loop simply fires faster once phase 2 is active.
- **Shockwave slam** — on its own longer timer. Implemented as an expanding radius rather than a persistent hazard: a coroutine grows `radius` over time and, each frame, checks whether the player's distance from the boss falls within a thin band around the current radius. If so it deals damage once (guarded by a `hitPlayer` flag) and stops checking. An optional visual prefab is scaled to `radius * 2` (diameter) if assigned, so the fight is still testable without art.
- **Spiral (phase 2 only)** — a few arms fired on a short interval with the base angle advanced each tick by `spiralRotationSpeed * interval`, so the safe gaps rotate around the room. Layered on top of the still-running radial bursts.

The phase transition is driven by overriding `TakeDamage`: it calls `base.TakeDamage` first (which does the actual subtraction and can trigger death), then checks whether HP has crossed below `phaseTwoThreshold` for the first time, guarded by a `phaseTransitionStarted` bool so it can only fire once. The transition plays a one-off "panic" telegraph — a stronger colour flush and a pause — then flips the phase and starts the spiral loop.

Fairness came down to telegraphs. Every attack flushes the sprite to a brighter colour for `telegraphDuration` (default half a second) before it fires, reusing the same idea as the spawn-stun tell on normal enemies. The telegraph, both attack cooldowns, projectile counts, spiral rotation speed, shockwave radius/speed/thickness, and the phase threshold are all serialized inspector fields so the whole fight can be balanced by playtesting without touching code.

Two follow-up fixes came out of playtesting. First, the radial bursts were invisible: every projectile in a ring spawns at the boss's exact centre, so they overlapped on the first frame and, because a projectile that touches anything other than its spawner, another enemy, or the player destroys itself, they mutually annihilated instantly. Added a guard in `EnemyProjectile.OnTriggerEnter2D` so projectiles ignore each other. The single-shot teleporter had never exposed this because it only spawns one.

Second, the shockwave slam was cut entirely. It was hard to read without dedicated art — the expanding-ring placeholder never showed clearly — and combined with the radial and spiral patterns it pushed the fight past fair into frustrating. Rather than sink more time into a visual for it, the whole attack (the `ShockwaveLoop`, `PerformShockwave`, its serialized fields, and the placeholder `ShockwaveRing` script) was removed.

The phase design was then simplified further: the radial burst is now phase 1 only. Running the faster radial burst and the rotating spiral simultaneously in phase 2 was too dense to dodge, so the two phases now read as a clean escalation of pattern rather than volume — a static ring of gaps to weave through in phase 1, swapped for a single rotating spiral whose safe gaps sweep around the room in phase 2. The `RadialBurstLoop` simply exits its `while` when the phase flips (which the transition sets after the panic telegraph), and the `SpiralLoop` takes over. The redundant phase-2 interval field was dropped.

Finally the telegraph presentation was changed from a colour flush to a dedicated sprite swap. A brighter tint read poorly against the boss's dark artwork, so both the per-attack tell and the one-off panic tell now display a distinct `telegraphSprite` (and an optional `panicSprite`, falling back to the former) for the telegraph duration, then return to the normal animation. This required a small addition to the base `Enemy` class: a `suppressFrameAnimation` flag, mirroring the existing `suppressScalePulse`. The base `Update` skips its two-frame flip while the flag is set, so the telegraph sprite is held steady instead of being overwritten on the next flip tick. When the tell ends the flag is cleared and the ordinary frame flipping resumes on its own. The colour fields (`telegraphColor`, `panicColor`) and the cached `baseColor` were removed in the process.

---

## 03.07.2026 — New Item: The Onion

Added a passive item, the Onion, to the item-room pool. It grants the player an automatic attack: a projectile is fired at the nearest enemy in the room, on its own timer, at half the player's attack speed. This was the first item that needed a player-owned projectile — until now the player's only attack was the melee lightning cone, so there was no projectile that damaged enemies. `EnemyProjectile` only damages the player, so a mirror-image `PlayerProjectile` was written: same consistent-velocity movement (velocity re-applied every `FixedUpdate`), but its trigger looks for an `Enemy` to damage and passes through the player, sibling shots, and incoming enemy fire.

The behaviour itself lives in an `AutoShooter` component. Rather than bake the auto-fire into `PlayerMovement` (where it would sit unused unless the item is picked up), the Onion attaches `AutoShooter` to the player on pickup. This keeps the ability self-contained and makes it trivial to gate behind the item — no dormant fields on the player. `OnionItem` derives from the same `Item` base class as the health item, so it inherits the whole pickup routine (the freeze, the fly-to-player animation, the trigger handling); its `Apply` just gets-or-adds the `AutoShooter` and hands it the projectile prefab. A second Onion simply re-points the existing shooter rather than stacking a second one.

Two details from the brief drove the design. The fire rate is "half the player's attack speed", so the cooldown is `1 / (attackSpeed * 0.5)` — read fresh each shot, so it automatically tracks any attack-speed upgrades the player picks up. And the projectile size scales with damage: each shot's scale multiplier is `currentDamage / baseDamage`, computed at spawn time, so because projectiles are fired repeatedly every new shot reflects the current damage. There was no need to resize anything in flight — the next projectile is simply bigger or smaller. Scaling the transform scales the collider with it, so a higher-damage Onion shot is also physically larger and easier to land.

The reference point for that ratio was originally a hardcoded `referenceDamage` of 10, but that duplicated a value already authored on the player prefab. It was replaced by capturing the player's starting damage into a `BaseDamage` property in `PlayerMovement.Start` — the damage as set in the inspector, before any room-clear upgrades — which `AutoShooter` divides by. Now the projectile is exactly its prefab size when the player is at their authored damage, whatever that inspector value happens to be, with no constant to keep in sync. A guard falls back to a 1x multiplier if `BaseDamage` is ever zero.

Target selection is a straightforward nearest-enemy search: `FindObjectsByType<Enemy>` and pick the minimum squared distance. Because only the current room's enemies are active at any time, this naturally scopes to "enemies in the room". If there are none, the shooter holds fire with its cooldown already elapsed, so it fires immediately the moment an enemy appears. The whole thing respects pause by bailing out of `Update` while `GameManager.IsPaused` is set.

---

## 04.07.2026 — Background Music

Added background music with two tracks: one for active gameplay and one for everything else (menu, options, leaderboard, game over). A small `MusicManager` singleton owns two `AudioSource` components and crossfades between them — fading one out while the other fades in — so track changes are smooth rather than a hard cut. It persists for the whole session via `DontDestroyOnLoad` and a standard instance guard.

The crossfade coroutine uses `Time.unscaledDeltaTime` deliberately, so the fade still progresses when the game is paused (which sets `Time.timeScale` to zero). The `Play` method is idempotent: it early-outs if asked to play the track that is already current, which means the state-transition hooks can call it liberally without restarting the music.

Muting did not need any new work. The existing mute button drives `AudioListener.volume` globally, and since the music plays through ordinary audio sources it is silenced along with everything else. The `MusicManager` deliberately never touches `AudioListener.volume` to avoid fighting the mute button.

Wiring it up was a matter of picking the two state transitions in `GameManager`: `PlayGameplay()` is called from `StartGame`, and `PlayMenu()` from `Start` (initial menu), `ShowGameOver` (run ends, win or lose), and `CleanupAndShowMenu` (return to menu). The options and leaderboard screens are menu-adjacent so they simply keep the menu track playing — no extra calls needed.

---

## 05.07.2026 — Enemy Death Particles

Added a death effect: a big puff of black smoke when an enemy dies. The hook lives in the base `Enemy` class, so every enemy type and the boss get it for free — a `deathEffectPrefab` field is instantiated in `Die()` just before the GameObject is destroyed. It is spawned unparented so it outlives the enemy and finishes its own animation rather than being destroyed along with it.

The interesting part was making the smoke visible without importing any art. A `DeathSmoke` script configures a `ParticleSystem` entirely in code — a single burst, an expanding size-over-lifetime curve, a slight upward drift, and an alpha fade — and then destroys itself via `ParticleSystemStopAction.Destroy` when the burst finishes. The default particle material is additive, and additive black is invisible (adding zero changes nothing), so the script builds an alpha-blended material instead using the legacy alpha-blended particle shader, tinted by a runtime-generated white disc texture.

The first version was invisible in-game, which came down to two things. The biggest was sorting layer: the game's sprites do not render on the built-in "Default" sorting layer but on a custom layer named "Player", so a particle system left on "Default" was drawn behind the floor. The fix was to set the renderer's `sortingLayerName` to "Player" with a high `sortingOrder` so the puff draws on top of the enemies. The second was the art style — the initial soft 64×64 radial gradient with bilinear filtering clashed with the pixel-art look and, being mostly transparent, read as a faint haze rather than smoke. It was replaced with a tiny 10×10 hard-edged disc set to `FilterMode.Point`, so it scales up as chunky blocks, and the particles were made opaque (holding full alpha until a late snap-fade) and dense, so the puff is clearly visible even against the dark dungeon floor. Texture and material are static and shared across all puffs, so repeated deaths don't allocate. The result is still a self-contained effect prefab — an empty object with a ParticleSystem and the `DeathSmoke` script — with no art assets and no manually tuned particle modules.

A final tuning pass shortened it to an actual puff: the first visible version was too large and lingered too long. The lifetime was cut to under half a second, the start size roughly halved, the particle count reduced, and the size-over-lifetime growth flattened so the particles no longer balloon before fading. Now it's a quick, small burst that pops and is gone.

---

## 06.07.2026 — Lightning Bolt Outline

Gave the player's lightning attack a black outline so it reads more clearly against the busy dungeon. The attack is drawn as a set of `LineRenderer` bolts (one per projectile, each a jagged midpoint-displaced line regenerated on a flicker). The outline is done the obvious way for a line: each bolt gets a second, parallel `LineRenderer` that is slightly thicker, solid black, and rendered one sorting order below it, so the coloured bolt sits on top of its own black silhouette.

The line-creation code was refactored into a `CreateLine` helper so the bolt and its outline are built the same way with different width, colour, and sorting order. The outline width is the bolt's width plus `outlineWidth` on each side, and its colour tapers to transparent at the tip exactly like the bolt, so the fade-out doesn't leave a black blob at the end. A `SetPoint` helper writes each regenerated vertex to both renderers at once, so the outline always traces the identical jagged path. Because all outlines share `sortingOrder - 1` and all bolts share `sortingOrder`, every bolt draws above every outline, so overlapping bolts in the cone don't get a black seam through them. The outline is toggleable and its width and colour are inspector-exposed.
