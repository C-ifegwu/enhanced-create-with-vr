using System.Collections.Generic;
using UnityEngine;
public class ContactLogger : MonoBehaviour
{
    public static HashSet<string> seen = new HashSet<string>();
    void OnCollisionStay(Collision c)
    {
        foreach (var cp in c.contacts)
            seen.Add(cp.thisCollider.name + " <-> " + c.collider.name + " (" + c.collider.GetType().Name + ", under " + (c.collider.transform.parent ? c.collider.transform.parent.name : "root") + ")");
    }
}
