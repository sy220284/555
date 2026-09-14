using System;
using System.Collections.Generic;
using ModernRA.Rules;
using ModernRA.Simulation;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace ModernRA.Presentation
{
    public sealed class GrayboxPlayableController : MonoBehaviour
    {
        private const int LocalPlayerId = 1;
        private const float SelectionRadiusPixels = 30f;
        private const float DragSelectionThresholdPixels = 8f;
        private const float PanSpeed = 1800f;
        private const float ZoomSpeed = 1400f;

        private readonly Dictionary<int, GameObject> _markers = new Dictionary<int, GameObject>();
        private readonly HashSet<int> _seenIds = new HashSet<int>();
        private readonly List<int> _removeIds = new List<int>();
        private readonly HashSet<int> _selectedUnitIds = new HashSet<int>();
        private readonly List<int> _orderedSelection = new List<int>();
        private readonly Dictionary<int, HashSet<int>> _localControlGroups = new Dictionary<int, HashSet<int>>();
        private Camera _camera;
        private EntityQuery _ruleEntityQuery;
        private EntityQuery _matchQuery;
        private EntityQuery _commandQueueQuery;
        private bool _ruleQueryReady;
        private RuntimeMapBootstrapData _map;
        private Int2[] _corridor = Array.Empty<Int2>();
        private GameObject _ground;
        private Material _teamOneMaterial;
        private Material _teamTwoMaterial;
        private Material _selectedMaterial;
        private Material _groundMaterial;
        private Vector2 _selectionStart;
        private bool _selectionDragging;
        private int _activeControlGroup;
        private int _nextSequence = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (GameObject.Find("ModernRA.GrayboxPlayable") != null)
                return;
            var root = new GameObject("ModernRA.GrayboxPlayable");
            root.AddComponent<GrayboxPlayableController>();
        }

        private void Start()
        {
            _map = GrayRangeGeneratedData.Create();
            _corridor = _map.CreateStandardAnnihilationConfig().SharedCorridor;
            CreateMaterials();
            EnsureGround();
            EnsureCamera();
        }

        private void Update()
        {
            EnsureCamera();
            UpdateCameraControls();

            EntityManager entityManager;
            if (!TryGetEntityManager(out entityManager))
                return;

            EnsureQueries(entityManager);
            SyncMarkers(entityManager);
            HandleSelection(entityManager);
            HandleUnitCommands(entityManager);
        }

        private void OnDestroy()
        {
            if (_ruleQueryReady)
            {
                _ruleEntityQuery.Dispose();
                _matchQuery.Dispose();
                _commandQueueQuery.Dispose();
            }
            foreach (GameObject marker in _markers.Values)
                if (marker != null) Destroy(marker);
            _markers.Clear();
            if (_ground != null) Destroy(_ground);
            DestroyMaterial(_teamOneMaterial);
            DestroyMaterial(_teamTwoMaterial);
            DestroyMaterial(_selectedMaterial);
            DestroyMaterial(_groundMaterial);
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(12f, 12f, 470f, 112f), "ModernRA 灰盒控制");
            GUI.Label(new Rect(24f, 38f, 440f, 20f), "左键/框选单位  |  Shift追加 / Ctrl排除  |  右键移动  |  H坚守 / R恢复");
            GUI.Label(new Rect(24f, 60f, 440f, 20f), "Ctrl+1—9建立编组  |  1—9选择编组  |  WASD移动视角  |  滚轮缩放");
            GUI.Label(new Rect(24f, 82f, 440f, 20f), $"当前选择：{_selectedUnitIds.Count}  当前编组：{(_activeControlGroup > 0 ? _activeControlGroup.ToString() : "无")}");

            if (_selectionDragging)
            {
                Vector2 current = Input.mousePosition;
                Rect screenRect = ScreenRect(_selectionStart, current);
                GUI.Box(new Rect(screenRect.xMin, Screen.height - screenRect.yMax, screenRect.width, screenRect.height), string.Empty);
            }

            EntityManager entityManager;
            if (!TryGetEntityManager(out entityManager))
                return;

            EnsureQueries(entityManager);
            if (!_matchQuery.IsEmptyIgnoreFilter)
            {
                AnnihilationMatchState state = _matchQuery.GetSingleton<AnnihilationMatchState>();
                GUI.Label(new Rect(24f, 104f, 440f, 20f), $"Tick {state.Tick}  我方单位 {state.TeamAAliveUnits}  敌方单位 {state.TeamBAliveUnits}  胜方 {state.WinnerTeamId}");
            }
        }

        private static bool TryGetEntityManager(out EntityManager entityManager)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                entityManager = default;
                return false;
            }
            entityManager = world.EntityManager;
            return true;
        }

        private void EnsureQueries(EntityManager entityManager)
        {
            if (_ruleQueryReady)
                return;
            _ruleEntityQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<AnnihilationRuleEntity>(),
                ComponentType.ReadOnly<AnnihilationRuleActive>(),
                ComponentType.ReadOnly<SimPosition>(),
                ComponentType.ReadOnly<HealthState>());
            _matchQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<AnnihilationMatchState>());
            _commandQueueQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<PlayerCommandQueueState>(),
                ComponentType.ReadWrite<PlayerCommandRequest>());
            _ruleQueryReady = true;
        }

        private void SyncMarkers(EntityManager entityManager)
        {
            _seenIds.Clear();
            using NativeArray<Entity> entities = _ruleEntityQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                AnnihilationRuleEntity ruleEntity = entityManager.GetComponentData<AnnihilationRuleEntity>(entity);
                SimPosition simPosition = entityManager.GetComponentData<SimPosition>(entity);
                _seenIds.Add(ruleEntity.StableId);

                GameObject marker;
                if (!_markers.TryGetValue(ruleEntity.StableId, out marker) || marker == null)
                {
                    marker = CreateMarker(ruleEntity);
                    _markers[ruleEntity.StableId] = marker;
                }

                float y = ruleEntity.EntityKind == 1 ? 60f : 22f;
                marker.transform.position = new Vector3(simPosition.Value.x, y, simPosition.Value.z);
                Renderer renderer = marker.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = _selectedUnitIds.Contains(ruleEntity.StableId)
                        ? _selectedMaterial
                        : ruleEntity.TeamId == 1 ? _teamOneMaterial : _teamTwoMaterial;
            }

            _removeIds.Clear();
            foreach (KeyValuePair<int, GameObject> pair in _markers)
                if (!_seenIds.Contains(pair.Key)) _removeIds.Add(pair.Key);
            for (int i = 0; i < _removeIds.Count; i++)
            {
                int id = _removeIds[i];
                if (_markers.TryGetValue(id, out GameObject marker) && marker != null)
                    Destroy(marker);
                _markers.Remove(id);
                _selectedUnitIds.Remove(id);
            }
        }

        private GameObject CreateMarker(AnnihilationRuleEntity ruleEntity)
        {
            GameObject marker = GameObject.CreatePrimitive(ruleEntity.EntityKind == 1 ? PrimitiveType.Cube : PrimitiveType.Capsule);
            marker.name = $"Graybox-{ruleEntity.StableId}";
            Collider collider = marker.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            marker.transform.localScale = ruleEntity.EntityKind == 1
                ? new Vector3(180f, 120f, 180f)
                : new Vector3(45f, 45f, 75f);
            return marker;
        }

        private void HandleSelection(EntityManager entityManager)
        {
            if (_camera == null)
                return;

            if (Input.GetMouseButtonDown(0))
            {
                _selectionStart = Input.mousePosition;
                _selectionDragging = true;
            }
            if (!_selectionDragging || !Input.GetMouseButtonUp(0))
                return;

            Vector2 end = Input.mousePosition;
            _selectionDragging = false;
            bool append = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool exclude = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (!append && !exclude) _selectedUnitIds.Clear();
            _activeControlGroup = 0;
            Rect selection = ScreenRect(_selectionStart, end);
            bool click = selection.width < DragSelectionThresholdPixels && selection.height < DragSelectionThresholdPixels;

            float bestDistanceSq = SelectionRadiusPixels * SelectionRadiusPixels;
            int selected = 0;
            using NativeArray<Entity> entities = _ruleEntityQuery.ToEntityArray(Allocator.Temp);
            Vector3 mouse = end;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                AnnihilationRuleEntity ruleEntity = entityManager.GetComponentData<AnnihilationRuleEntity>(entity);
                if (ruleEntity.TeamId != LocalPlayerId || ruleEntity.EntityKind != 2)
                    continue;
                SimPosition position = entityManager.GetComponentData<SimPosition>(entity);
                Vector3 screen = _camera.WorldToScreenPoint(new Vector3(position.Value.x, 25f, position.Value.z));
                if (screen.z <= 0f)
                    continue;
                if (!click && selection.Contains(new Vector2(screen.x, screen.y)))
                {
                    if (exclude) _selectedUnitIds.Remove(ruleEntity.StableId);
                    else _selectedUnitIds.Add(ruleEntity.StableId);
                    continue;
                }
                if (!click) continue;
                float dx = screen.x - mouse.x;
                float dy = screen.y - mouse.y;
                float distanceSq = dx * dx + dy * dy;
                if (distanceSq >= bestDistanceSq)
                    continue;
                bestDistanceSq = distanceSq;
                selected = ruleEntity.StableId;
            }
            if (selected > 0)
            {
                if (exclude) _selectedUnitIds.Remove(selected);
                else _selectedUnitIds.Add(selected);
            }
        }

        private void HandleUnitCommands(EntityManager entityManager)
        {
            HandleControlGroupKeys(entityManager);
            if (_selectedUnitIds.Count == 0) return;

            if (Input.GetMouseButtonDown(1) && TryGetGroundPoint(out Vector3 point))
            {
                int waypoint = FindNearestWaypoint(point);
                IssueWaypoint(entityManager, waypoint);
            }
            if (Input.GetKeyDown(KeyCode.H))
                IssueHolding(entityManager, true);
            if (Input.GetKeyDown(KeyCode.R))
                IssueHolding(entityManager, false);
        }

        private void HandleControlGroupKeys(EntityManager entityManager)
        {
            bool assign = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            for (int groupId = 1; groupId <= 9; groupId++)
            {
                if (!Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + groupId))) continue;
                if (assign && _selectedUnitIds.Count > 0)
                {
                    var groupCopy = new HashSet<int>(_selectedUnitIds);
                    _localControlGroups[groupId] = groupCopy;
                    FillOrderedSelection(groupCopy);
                    for (int i = 0; i < _orderedSelection.Count; i++)
                        EnqueueCommand(entityManager, PrototypePlayerCommandKind.AssignUnitToControlGroup,
                            PrototypeControlGroupPayload.EncodeUnitGroup(_orderedSelection[i], groupId));
                    _activeControlGroup = groupId;
                }
                else if (_localControlGroups.TryGetValue(groupId, out HashSet<int> storedGroup))
                {
                    _selectedUnitIds.Clear();
                    foreach (int unitId in storedGroup)
                        if (_seenIds.Contains(unitId)) _selectedUnitIds.Add(unitId);
                    _activeControlGroup = _selectedUnitIds.Count > 0 ? groupId : 0;
                }
                return;
            }
        }

        private void IssueWaypoint(EntityManager entityManager, int waypoint)
        {
            if (_activeControlGroup > 0)
            {
                EnqueueCommand(entityManager, PrototypePlayerCommandKind.SetControlGroupWaypoint,
                    PrototypeControlGroupPayload.EncodeGroupWaypoint(_activeControlGroup, waypoint));
                return;
            }
            FillOrderedSelection(_selectedUnitIds);
            for (int i = 0; i < _orderedSelection.Count; i++)
                EnqueueCommand(entityManager, PrototypePlayerCommandKind.SetUnitWaypoint,
                    PrototypeUnitWaypointPayload.Encode(_orderedSelection[i], waypoint));
        }

        private void IssueHolding(EntityManager entityManager, bool holding)
        {
            if (_activeControlGroup > 0)
            {
                EnqueueCommand(entityManager, holding ? PrototypePlayerCommandKind.HoldControlGroup : PrototypePlayerCommandKind.ResumeControlGroup,
                    _activeControlGroup);
                return;
            }
            FillOrderedSelection(_selectedUnitIds);
            for (int i = 0; i < _orderedSelection.Count; i++)
                EnqueueCommand(entityManager, holding ? PrototypePlayerCommandKind.HoldUnit : PrototypePlayerCommandKind.ResumeUnit,
                    _orderedSelection[i]);
        }

        private void FillOrderedSelection(IEnumerable<int> source)
        {
            _orderedSelection.Clear();
            _orderedSelection.AddRange(source);
            _orderedSelection.Sort();
        }

        private void EnqueueCommand(EntityManager entityManager, PrototypePlayerCommandKind kind, int intValue)
        {
            if (_commandQueueQuery.IsEmptyIgnoreFilter) return;

            Entity queueEntity = _commandQueueQuery.GetSingletonEntity();
            PlayerCommandQueueState state = entityManager.GetComponentData<PlayerCommandQueueState>(queueEntity);
            int sequence = Math.Max(_nextSequence, state.LastSequence + 1);
            _nextSequence = checked(sequence + 1);
            DynamicBuffer<PlayerCommandRequest> buffer = entityManager.GetBuffer<PlayerCommandRequest>(queueEntity);
            buffer.Add(new PlayerCommandRequest
            {
                PlayerId = LocalPlayerId,
                Sequence = sequence,
                Kind = (byte)kind,
                IntValue = intValue
            });
        }

        private static Rect ScreenRect(Vector2 a, Vector2 b)
        {
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        private bool TryGetGroundPoint(out Vector3 point)
        {
            point = default;
            if (_camera == null)
                return false;
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float distance))
                return false;
            point = ray.GetPoint(distance);
            return true;
        }

        private int FindNearestWaypoint(Vector3 point)
        {
            int selected = 0;
            float bestDistanceSq = float.MaxValue;
            for (int i = 0; i < _corridor.Length; i++)
            {
                float dx = _corridor[i].X - point.x;
                float dz = _corridor[i].Y - point.z;
                float distanceSq = dx * dx + dz * dz;
                if (distanceSq < bestDistanceSq)
                {
                    bestDistanceSq = distanceSq;
                    selected = i;
                }
            }
            return selected;
        }

        private void EnsureCamera()
        {
            if (_camera != null)
                return;
            _camera = Camera.main;
            if (_camera == null)
            {
                var cameraObject = new GameObject("ModernRA Strategy Camera");
                _camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            float width = _map != null ? _map.SizeMeters.X : 8000f;
            float height = _map != null ? _map.SizeMeters.Y : 8000f;
            float maxSize = Mathf.Max(width, height);
            _camera.transform.position = new Vector3(width * 0.5f, maxSize * 0.68f, height * 0.08f);
            _camera.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            _camera.nearClipPlane = 5f;
            _camera.farClipPlane = maxSize * 3f;
        }

        private void UpdateCameraControls()
        {
            if (_camera == null)
                return;
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 position = _camera.transform.position;
            position += new Vector3(horizontal, 0f, vertical) * (PanSpeed * Time.unscaledDeltaTime);

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
                position += _camera.transform.forward * (scroll * ZoomSpeed * Time.unscaledDeltaTime * 12f);

            float width = _map != null ? _map.SizeMeters.X : 8000f;
            float height = _map != null ? _map.SizeMeters.Y : 8000f;
            position.x = Mathf.Clamp(position.x, 0f, width);
            position.z = Mathf.Clamp(position.z, -height * 0.25f, height);
            position.y = Mathf.Clamp(position.y, 350f, Mathf.Max(width, height) * 1.4f);
            _camera.transform.position = position;
        }

        private void EnsureGround()
        {
            if (_ground != null)
                return;
            _ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _ground.name = "Graybox Ground";
            float width = _map.SizeMeters.X;
            float height = _map.SizeMeters.Y;
            _ground.transform.position = new Vector3(width * 0.5f, -2f, height * 0.5f);
            _ground.transform.localScale = new Vector3(width / 10f, 1f, height / 10f);
            Collider collider = _ground.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            Renderer renderer = _ground.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = _groundMaterial;
        }

        private void CreateMaterials()
        {
            _teamOneMaterial = CreateMaterial(new Color(0.15f, 0.45f, 0.95f));
            _teamTwoMaterial = CreateMaterial(new Color(0.9f, 0.2f, 0.18f));
            _selectedMaterial = CreateMaterial(new Color(1f, 0.82f, 0.12f));
            _groundMaterial = CreateMaterial(new Color(0.24f, 0.27f, 0.22f));
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");
            var material = new Material(shader);
            material.color = color;
            return material;
        }

        private static void DestroyMaterial(Material material)
        {
            if (material != null) Destroy(material);
        }
    }
}
