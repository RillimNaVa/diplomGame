using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace VoidSurvivor.ProceduralArena.Navigation
{
    /// <summary>
    /// Runtime NavMesh baker for a single-arena ArenaRoot. Adds a
    /// NavMeshSurface to the root if missing and rebuilds asynchronously
    /// under the arena-flow fade so the bake cost is invisible.
    /// Lifecycle: NavMeshSurface.OnDisable removes the NavMesh instance
    /// automatically when ArenaRoot is destroyed — no manual cleanup needed.
    /// </summary>
    public static class ArenaNavMeshController
    {
        public static NavMeshSurface EnsureSurface(GameObject arenaRoot)
        {
            if (arenaRoot == null) return null;
            var surface = arenaRoot.GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                surface = arenaRoot.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.Children;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                surface.layerMask = ~0;
            }
            return surface;
        }

        public static IEnumerator BakeAsync(GameObject arenaRoot)
        {
            if (arenaRoot == null) yield break;
            var surface = EnsureSurface(arenaRoot);
            if (surface == null) yield break;

            if (surface.navMeshData == null)
            {
                surface.navMeshData = new NavMeshData();
                surface.AddData();
            }

            var op = surface.UpdateNavMesh(surface.navMeshData);
            while (op != null && !op.isDone) yield return null;
            // Give one extra frame so NavMeshAgents placed this frame see the mesh.
            yield return null;
        }
    }
}
