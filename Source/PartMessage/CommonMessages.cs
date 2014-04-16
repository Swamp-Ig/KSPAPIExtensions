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
    [PartMessage(isAbstract: true)]
    public delegate void PartPhysicsChanged();

    /// <summary>
    /// Message for when the part's mass is modified.
    /// </summary>
    [PartMessage(typeof(PartPhysicsChanged))]
    public delegate void PartMassChanged();

    /// <summary>
    /// Message for when the part's CoMOffset changes.
    /// </summary>
    [PartMessage(typeof(PartPhysicsChanged))]
    public delegate void PartCoMOffsetChanged();

    /// <summary>
    /// Message for when the part's moments of intertia change.
    /// </summary>
    [PartMessage(typeof(PartPhysicsChanged))]
    public delegate void PartMomentsChanged();


    /// <summary>
    /// Message for when the part's resource list is modified in some way.
    /// </summary>
    [PartMessage(isAbstract: true)]
    public delegate void PartResourcesChanged();

    /// <summary>
    /// Message for when the part's resource list is modified (added to or subtracted from).
    /// </summary>
    [PartMessage(typeof(PartResourcesChanged))]
    public delegate void PartResourceListChanged();

    /// <summary>
    /// Message for when the max amount of a resource is modified.
    /// </summary>
    [PartMessage(typeof(PartResourcesChanged))]
    public delegate void PartResourceMaxAmountChanged(PartResource resource);

    /// <summary>
    /// Message for when the initial amount of a resource is modified (only raised in the editor)
    /// </summary>
    [PartMessage(typeof(PartResourcesChanged))]
    public delegate void PartResourceInitialAmountChanged(PartResource resource);

    /// <summary>
    /// Message for when some change has been made to the part's rendering model.
    /// </summary>
    [PartMessage]
    public delegate void PartModelChanged();

    /// <summary>
    /// Message for when some change has been made to the part's collider.
    /// </summary>
    [PartMessage]
    public delegate void PartColliderChanged();
}
