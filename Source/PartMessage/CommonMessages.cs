using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using System.Linq.Expressions;
using System.Collections;
using System.Text.RegularExpressions;

namespace KSPAPIExtensions.PartMessage
{
    /// <summary>
    /// Listen for this to get notification when any physical constant is changed
    /// including the mass, CoM, moments of inertia, boyancy, ect.
    /// </summary>
    [PartMessageDelegate(isAbstract: true)]
    public delegate void PartPhysicsChanged();

    /// <summary>
    /// Message for when the part's mass is modified.
    /// </summary>
    [PartMessageDelegate(typeof(PartPhysicsChanged))]
    public delegate void PartMassChanged([UseLatest] float mass);

    /// <summary>
    /// Message for when the part's CoMOffset changes.
    /// </summary>
    [PartMessageDelegate(typeof(PartPhysicsChanged))]
    public delegate void PartCoMOffsetChanged([UseLatest] Vector3 offset);

    /// <summary>
    /// Message for when the part's moments of intertia change.
    /// </summary>
    [PartMessageDelegate(typeof(PartPhysicsChanged))]
    public delegate void PartMomentsChanged([UseLatest] Vector3 intertiaTensor, [UseLatest] Quaternion intertiaTensorRotation);


    /// <summary>
    /// Message for when the part's resource list is modified in some way.
    /// </summary>
    [PartMessageDelegate(isAbstract: true)]
    public delegate void PartResourcesChanged();

    /// <summary>
    /// Message for when the part's resource list is modified (added to or subtracted from).
    /// </summary>
    [PartMessageDelegate(typeof(PartResourcesChanged))]
    public delegate void PartResourceListChanged();

    /// <summary>
    /// Message for when the max amount of a resource is modified.
    /// </summary>
    [PartMessageDelegate(typeof(PartResourcesChanged))]
    public delegate void PartResourceMaxAmountChanged(string resource, [UseLatest] double maxAmount);

    /// <summary>
    /// Message for when the initial amount of a resource is modified (only raised in the editor)
    /// </summary>
    [PartMessageDelegate(typeof(PartResourcesChanged))]
    public delegate void PartResourceInitialAmountChanged(string resource, [UseLatest] double amount);

    /// <summary>
    /// Message for when some change has been made to the part's rendering model.
    /// </summary>
    [PartMessageDelegate]
    public delegate void PartModelChanged();

    /// <summary>
    /// Message for when some change has been made to the part's collider.
    /// </summary>
    [PartMessageDelegate]
    public delegate void PartColliderChanged();
}
