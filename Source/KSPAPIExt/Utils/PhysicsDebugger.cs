using System.Text;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace KSPAPIExtensions.DebuggingUtils
{

    /// <summary>
    /// Part module that dumps out the physics constants pertaining to the part every 10 seconds.
    /// </summary>
    public class ModulePhysicsDebugger : PartModule
    {

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Physics Debugger")]
        private double lastFixedUpdate;

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight && (Time.time - lastFixedUpdate) > 10)
            {
                lastFixedUpdate = Time.time;

                Transform rootT = part.vessel.rootPart.transform;

                StringBuilder sb = new StringBuilder();

                float massTotal = 0;
                Vector3 wtSum = Vector3.zero;

                foreach (Part p in part.vessel.parts)
                {
                    sb.AppendLine(p.name + " position(wrt root)=" + rootT.InverseTransformPoint(p.transform.position).ToString("F5"));

                    if (p.rigidbody != null)
                    {
                        massTotal += p.rigidbody.mass;
                        wtSum += p.rigidbody.mass * rootT.InverseTransformPoint(p.transform.TransformPoint(p.rigidbody.centerOfMass));
                        sb.AppendLine(p.name + " mass=" + p.rigidbody.mass);
                        if (p.rigidbody.centerOfMass != Vector3.zero)
                            sb.AppendLine(p.name + " CoM offset=" + p.rigidbody.centerOfMass.ToString("F5"));
                        sb.AppendLine(p.name + " inertia tensor=" + p.rigidbody.inertiaTensor.ToString("F5") + " rotation=" + p.rigidbody.inertiaTensorRotation.ToStringAngleAxis("F5"));

                        foreach(Joint j in p.gameObject.GetComponents<Joint>())
                        {
                            sb.AppendLine(p.name + " joint type=" + j.GetType() + " position=" + j.anchor.ToString("F5") + " force=" + j.breakForce + " torque=" + j.breakTorque);
                        }
                    }

                    if (p.Modules.Contains("ModuleEngines"))
                    {
                        ModuleEngines mE = (ModuleEngines)p.Modules["ModuleEngines"];
                        foreach (Transform t in mE.thrustTransforms)
                        {
                            sb.AppendLine(p.name + " thrust transform position=" + t.position.ToString("F5"));
                        }
                    }
                }
                if (massTotal > 0)
                {
                    sb.AppendLine("CoM = " + (wtSum / massTotal).ToString("F5"));
                }
                Debug.Log(sb);
            }
        }

    }
}