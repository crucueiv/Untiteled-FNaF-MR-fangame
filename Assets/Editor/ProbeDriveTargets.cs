using System.Linq;
using System.Text;
using UnityEngine;

public static class ProbeDriveTargets
{
    public static string Execute()
    {
        var showcase = GameObject.Find("DriveCharacterShowcase");
        if (!showcase) return "Showcase no encontrado";
        var sb = new StringBuilder();
        foreach (Transform root in showcase.transform)
        {
            if (!root.Find("IKTargets")) continue;
            Transform B(string n) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == n && !t.GetComponent<Renderer>());
            var head = B("Head"); var neck = B("Neck"); var look = root.Find("IKTargets/LookAtTarget");
            if (!head || !look) { sb.AppendLine($"{root.name}: head={head} look={look}"); continue; }
            var dir = (look.position - head.position);
            var fwdAngle = Vector3.Angle(root.forward, dir);
            sb.AppendLine($"{root.name}: scale={root.lossyScale.x:F4} head={head.position:F3} neck={(neck ? neck.position.ToString("F3") : "-")} look={look.position:F3} " +
                          $"dist={dir.magnitude:F3} angFromForward={fwdAngle:F1} headFwdVsDir={Vector3.Angle(head.forward, dir):F1}");
        }
        return sb.ToString();
    }
}
