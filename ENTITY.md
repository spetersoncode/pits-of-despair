     ╭─────────────────────────────────────────────────────────────────────╮
     │ Implementation Plan: Component-Based Entity System with Goblins     │
     │                                                                     │
     │ 1. Create Component Data Resources                                  │
     │                                                                     │
     │ New: Scripts/Data/MovementData.cs                                   │
     │ - Godot Resource for movement configuration                         │
     │ - Future properties: speed, movement types, etc.                    │
     │ - Currently minimal but maintains consistent pattern                │
     │                                                                     │
     │ 2. Create EntityData Resource System                                │
     │                                                                     │
     │ New: Scripts/Data/EntityData.cs                                     │
     │ - Core properties: Name (string), Glyph (char), GlyphColor (Color)  │
     │ - Optional component data: Movement (MovementData, nullable)        │
     │ - Null = component not added to entity                              │
     │                                                                     │
     │ New: Resources/Entities/goblin.tres                                 │
     │ - Name="Goblin", Glyph='g', GlyphColor=Gray(0.5,0.5,0.5)            │
     │ - Movement = new MovementData() (non-null, gets component)          │
     │                                                                     │
     │ 3. Create Base Entity System                                        │
     │                                                                     │
     │ New: Scripts/Entities/BaseEntity.cs                                 │
     │ - Concrete class inheriting from Node2D                             │
     │ - Properties: GridPosition, Glyph (char), GlyphColor (Color)        │
     │ - Signal: PositionChanged(GridPosition newPos)                      │
     │ - Components attached as children                                   │
     │                                                                     │
     │ 4. Create MovementComponent                                         │
     │                                                                     │
     │ New: Scripts/Components/MovementComponent.cs                        │
     │ - Node component for movement behavior                              │
     │ - Signal: MoveRequested(Vector2I direction)                         │
     │ - Method: RequestMove(Vector2I direction)                           │
     │                                                                     │
     │ 5. Create MovementSystem                                            │
     │                                                                     │
     │ New: Scripts/Systems/MovementSystem.cs                              │
     │ - Has MapSystem reference (only system that needs it)               │
     │ - Subscribes to all MovementComponent MoveRequested signals         │
     │ - Validates via MapSystem.IsWalkable(targetPos)                     │
     │ - Updates entity.GridPosition on valid move                         │
     │ - Entity emits PositionChanged after update                         │
     │                                                                     │
     │ 6. Create EntityFactory                                             │
     │                                                                     │
     │ New: Scripts/Systems/EntityFactory.cs                               │
     │ - CreateEntity(EntityData data, GridPosition pos)                   │
     │ - Sets core properties (glyph, color, position)                     │
     │ - Checks data.Movement != null → creates and adds MovementComponent │
     │ - Returns configured entity node                                    │
     │                                                                     │
     │ 7. Refactor Player                                                  │
     │                                                                     │
     │ Modify: Scripts/Entities/Player.cs                                  │
     │ - Inherit from BaseEntity instead of Node2D                         │
     │ - Change Glyph from string to char ('@')                            │
     │ - Add MovementComponent as child node                               │
     │ - TryMove(direction) calls MovementComponent.RequestMove(direction) │
     │ - Keep TurnCompleted signal for future turn system                  │
     │                                                                     │
     │ 8. Create EntityManager                                             │
     │                                                                     │
     │ New: Scripts/Systems/EntityManager.cs                               │
     │ - Manages all non-player entities (List)                            │
     │ - SpawnGoblins(List<List<GridPosition>> roomTiles)                  │
     │   - Loads goblin.tres resource                                      │
     │   - For each room: spawns 1-3 goblins at random floor tiles         │
     │   - Uses EntityFactory.CreateEntity()                               │
     │ - Signals: EntityAdded(BaseEntity), EntityRemoved(BaseEntity)       │
     │ - Provides GetAllEntities() for renderer                            │
     │                                                                     │
     │ 9. Extend MapSystem                                                 │
     │                                                                     │
     │ Modify: Scripts/Systems/MapSystem.cs                                │
     │ - Add GetRoomFloorTiles() returning List<List<GridPosition>>        │
     │ - Store reference to BSPNode tree during generation                 │
     │ - Traverse leaf nodes to collect floor tiles per room               │
     │ - Enables per-room entity spawning                                  │
     │                                                                     │
     │ 10. Update ASCIIRenderer                                            │
     │                                                                     │
     │ Modify: Scripts/Systems/ASCIIRenderer.cs                            │
     │ - Subscribe to EntityManager EntityAdded/EntityRemoved signals      │
     │ - Maintain internal list of entities to render                      │
     │ - Render order: tiles → entities → player on top                    │
     │ - Draw entities at GridPosition with their glyph/color              │
     │                                                                     │
     │ 11. Wire Systems in GameLevel                                       │
     │                                                                     │
     │ Modify: Scripts/Systems/GameLevel.cs + Scenes/Main/GameLevel.tscn   │
     │ - Add nodes: MovementSystem, EntityManager, EntityFactory           │
     │ - Wire connections:                                                 │
     │   - MovementSystem.SetMapSystem(MapSystem)                          │
     │   - Player.MovementComponent → MovementSystem                       │
     │   - EntityManager → ASCIIRenderer for rendering                     │
     │   - EntityManager → MovementSystem for goblin movement components   │
     │ - Initialization flow:                                              │
     │   - MapSystem generates map                                         │
     │   - Player spawns                                                   │
     │   - EntityManager spawns goblins                                    │
     │   - All signals connected                                           │
     │                                                                     │
     │ Result                                                              │
     │                                                                     │
     │ - ✅ Data-driven entity creation (scales to hundreds)               │
     │ - ✅ Component-based architecture (MovementData/Component pattern)  │
     │ - ✅ Event-driven movement (no MapSystem in entities)               │
     │ - ✅ Type-safe char glyphs with full Unicode support                │
     │ - ✅ 1-3 gray 'g' goblins per room (stationary for now)             │
     │ - ✅ Foundation for future: HealthComponent, AIComponent, etc.      │
     ╰─────────────────────────────────────────────────────────────────────╯
