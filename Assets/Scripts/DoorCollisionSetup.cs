using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// The glass door leaf sits inside the frame's mesh collider and just above the floor boxes.
/// The hinge joint already keeps the leaf in place, so collisions with the frame and the floor are
/// ignored. That lets the leaf swing freely while it still collides with props and the player's hands.
/// </summary>
[DefaultExecutionOrder(-50)]
public class DoorCollisionSetup : MonoBehaviour
{
    void Awake()
    {
        var own = GetComponentsInChildren<Collider>(true);
        var ignore = new System.Collections.Generic.List<Collider>();
        // Frame and parent colliders (the frame's mesh collider lives on the parent object).
        foreach (var p in GetComponentsInParent<Collider>(true))
            if (!p.transform.IsChildOf(transform)) ignore.Add(p);
        // Floor boxes that overlap the bottom of the leaf.
        foreach (var o in Physics.OverlapBox(transform.position + Vector3.up * 1.5f, new Vector3(1.4f, 1.6f, 0.4f)))
            if (!o.isTrigger && !o.transform.IsChildOf(transform) && o.attachedRigidbody == null && o.name.StartsWith("floor"))
                ignore.Add(o);
        // Teleportation areas and anchors are only raycast targets for teleporting, they must never block the door.
        foreach (var tp in FindObjectsByType<BaseTeleportationInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            foreach (var c in tp.GetComponentsInChildren<Collider>(true)) ignore.Add(c);
        foreach (var a in own)
            foreach (var b in ignore)
                if (a != null && b != null) Physics.IgnoreCollision(a, b, true);
    }
}
