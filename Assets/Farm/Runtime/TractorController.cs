using System.Collections.Generic;
using NextGenFarm.Core;
using UnityEngine;

namespace NextGenFarm
{
    public sealed class TractorController : MonoBehaviour
    {
        public FarmAction SelectedAction { get; set; }
        public string SelectedCrop { get; set; } = "carrot";
        public bool Mounted { get; private set; }
        public bool Automatic { get; private set; }
        public int LastAffected { get; private set; }
        public string NavigationStatus { get; private set; } = "Manual";
        public Vector2Int TargetCell => Automatic ? grid.WorldToCell(transform.position) :
            grid.WorldToCell(transform.position + transform.forward * grid.CellSize);
        private FarmGridManager grid;
        private GameObject walkingVisual, tractorVisual;
        private List<Cell> route, path;
        private int routeIndex, pathIndex;
        private float nextAction;
        public void Initialize(FarmGridManager farm, GameObject person, GameObject tractor)
        {
            grid = farm; walkingVisual = person; tractorVisual = tractor;
            SetMounted(false);
        }
        public void SetMounted(bool mounted)
        {
            StopAuto(); Mounted = mounted;
            walkingVisual.SetActive(!mounted); tractorVisual.SetActive(mounted);
        }
        public void StopAuto() { Automatic = false; path = null; NavigationStatus = "Manual"; }
        public void UseTool()
        {
            if (Time.unscaledTime < nextAction) return;
            if (Automatic) StopAuto();
            nextAction = Time.unscaledTime + 0.2f;
            LastAffected = grid.Act(TargetCell, SelectedAction, SelectedCrop, Mounted);
        }
        public void ToggleAuto()
        {
            if (Automatic) { StopAuto(); return; }
            if (!Mounted) SetMounted(true);
            route = GridNavigation.Serpentine(grid.World.Width, grid.World.Depth);
            routeIndex = pathIndex = 0; path = null; Automatic = true;
            NavigationStatus = "Auto: scanning rows";
        }
        private void Update()
        {
            if (grid == null || !FarmGameFlow.GameplayAllowed) return;
            if (FarmInput.Press('1')) SelectedAction = FarmAction.Plow;
            if (FarmInput.Press('2')) SelectedAction = FarmAction.Sow;
            if (FarmInput.Press('3')) SelectedAction = FarmAction.Water;
            if (FarmInput.Press('4')) SelectedAction = FarmAction.Harvest;
            if (FarmInput.Press('t')) SetMounted(!Mounted);
            if (FarmInput.Press('r')) ToggleAuto();
            Vector2 input = Vector2.ClampMagnitude(FarmInput.Move, 1);
            if (input.sqrMagnitude > 0.01f)
            {
                StopAuto();
                // Project camera forward onto the ground; movement follows the free-look view.
                Vector3 forward=Camera.main!=null?Camera.main.transform.forward:Vector3.forward;forward.y=0;forward.Normalize();
                Vector3 move = forward*input.y + Vector3.Cross(Vector3.up,forward)*input.x;
                Vector3 desired = transform.position + move * ((Mounted ? 7 : 4) * Time.deltaTime);
                var from = grid.WorldToCell(transform.position); var to = grid.WorldToCell(desired);
                // Substeps prevent tunnelling over reserved cells at low frame rates.
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(transform.position, desired) / (grid.CellSize * 0.25f)));
                Vector3 origin = transform.position;
                for (int s = 1; s <= steps; s++)
                {
                    Vector3 candidate = Vector3.Lerp(origin, desired, (float)s / steps);
                    to = grid.WorldToCell(candidate); from = grid.WorldToCell(transform.position);
                    if (!GridNavigation.Passable(grid.World, new Cell(from.x, from.y), new Cell(to.x, to.y), 0.75f)) break;
                    // Avoid diagonal corner cutting.
                    if (from.x != to.x && from.y != to.y &&
                        (!grid.CanDrive(to.x, from.y) || !grid.CanDrive(from.x, to.y))) break;
                    candidate.y = grid.CellCenter(to.x, to.y).y; transform.position = candidate;
                }
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), 12 * Time.deltaTime);
            }
            if (Automatic) TickAuto();
            else if (FarmInput.ActionHeld && Time.unscaledTime >= nextAction)
                UseTool();
        }
        private void TickAuto()
        {
            if (routeIndex >= route.Count) { StopAuto(); NavigationStatus = "Auto complete"; return; }
            if (path == null)
            {
                var from = grid.WorldToCell(transform.position);
                path = GridNavigation.FindPath(grid.World, new Cell(from.x, from.y), route[routeIndex]);
                pathIndex = 0;
                if (path == null) { routeIndex++; NavigationStatus = "Skipped unreachable row"; return; }
                // Include start center to align footprint even if the path has zero steps.
                path.Insert(0, new Cell(from.x, from.y));
            }
            if (pathIndex < path.Count)
            {
                var target = path[pathIndex];
                var current = grid.WorldToCell(transform.position);
                if (!GridNavigation.Passable(grid.World, new Cell(current.x, current.y), target, 0.75f))
                { path = null; routeIndex++; return; }
                Vector3 destination = grid.CellCenter(target.x, target.z);
                Vector3 delta = destination - transform.position;
                Vector3 horizontal = new Vector3(delta.x, 0, delta.z);
                if (horizontal.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(horizontal), 8 * Time.deltaTime);
                transform.position = Vector3.MoveTowards(transform.position, destination, 7 * Time.deltaTime);
                if ((transform.position - destination).sqrMagnitude < 0.0001f) pathIndex++;
                return;
            }
            if (Time.unscaledTime < nextAction) return;
            nextAction = Time.unscaledTime + 0.2f;
            LastAffected = grid.Act(new Vector2Int(route[routeIndex].x, route[routeIndex].z), SelectedAction, SelectedCrop, true);
            NavigationStatus = "Auto " + (routeIndex + 1) + "/" + route.Count;
            routeIndex++; path = null;
        }
        private void OnDrawGizmosSelected()
        {
            if (grid == null) return;
            var c = TargetCell;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(grid.CellCenter(c.x, c.y) + Vector3.up * 0.1f,
                new Vector3(grid.CellSize, 0.1f, grid.CellSize) * (Mounted ? 3 : 1));
        }
    }
}
