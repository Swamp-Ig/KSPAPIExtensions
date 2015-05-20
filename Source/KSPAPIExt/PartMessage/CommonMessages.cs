using UnityEngine;

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
    /// When the the volume of some space within the part changes, this message is raised
    /// </summary>
    /// <param name="name">The name of the area. This may be one of the names of the <see cref="PartVolumes"/> enum for a 'standard volume', or some custom value which obviously the sender and reciever will need to agree on.</param>
    /// <param name="volume">The volume in cubic meters (kilolitres)</param>
    [PartMessageDelegate]
    public delegate void PartVolumeChanged(string name, [UseLatest] float volume);

    /// <summary>
    /// Well known volumes within a part.
    /// </summary>
    public enum PartVolumes
    {
        /// <summary>
        /// Tankage - the volume devoted to storage of fuel, life support resources, ect
        /// </summary>
        Tankage,
        /// <summary>
        /// The volume devoted to habitable space.
        /// </summary>
        Habitable,
    }

    /// <summary>
    /// Abstract message - some change to an attach node has occured.
    /// </summary>
    [PartMessageDelegate(isAbstract: true)]
    public delegate void PartAttachNodeChanged(AttachNode node);

    /// <summary>
    /// Raised when the size of an attachment node is changed.
    /// </summary>
    /// <param name="node">The attachment node</param>
    /// <param name="minDia">The minimum diameter across the attachment area. For circular areas this will be the diameter</param>
    /// <param name="area">Area in square meters of the attachment. </param>
    [PartMessageDelegate(typeof(PartAttachNodeChanged))]
    public delegate void PartAttachNodeSizeChanged(AttachNode node, [UseLatest] float minDia, [UseLatest] float area);

    /// <summary>
    /// Location or orientation of the attachment node is changed
    /// </summary>
    /// <param name="node">The attachment node</param>
    [PartMessageDelegate(typeof(PartAttachNodeChanged))]
    public delegate void PartAttachNodePositionChanged(AttachNode node, [UseLatest] Vector3 location, [UseLatest] Vector3 orientation, [UseLatest] Vector3 secondaryAxis);

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
    public delegate void PartResourceMaxAmountChanged(PartResource resource, [UseLatest] double maxAmount);

    /// <summary>
    /// Message for when the initial amount of a resource is modified (only raised in the editor)
    /// </summary>
    [PartMessageDelegate(typeof(PartResourcesChanged))]
    public delegate void PartResourceInitialAmountChanged(PartResource resource, [UseLatest] double amount);

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

    /// <summary>
    /// There has been some change to the part heirachy.
    /// </summary>
    [PartMessageDelegate(isAbstract:true)]
    public delegate void PartHeirachyChanged();

    /// <summary>
    /// A root part has been selected in the VAB.
    /// Note: this is automatically invoked by the framework, so you don't ever need to raise the event.
    /// </summary>
    [PartMessageDelegate(typeof(PartHeirachyChanged))]
    public delegate void PartRootSelected();

    /// <summary>
    /// A root part has been removed in the VAB.
    /// Note: this is automatically invoked by the framework, so you don't ever need to raise the event.
    /// </summary>
    [PartMessageDelegate(typeof(PartHeirachyChanged))]
    public delegate void PartRootRemoved();

    /// <summary>
    /// The parent of this part has changed. Note: this is automatically invoked by the framework, so you don't ever need to raise the event.
    /// </summary>
    /// <param name="parent">New parent, or null if it has been detached.</param>
    [PartMessageDelegate(typeof(PartHeirachyChanged))]
    public delegate void PartParentChanged([UseLatest] Part parent);

    /// <summary>
    /// New child has been attached to this part. Note: this is automatically invoked by the framework, so you don't ever need to raise the event.
    /// </summary>
    [PartMessageDelegate(typeof(PartHeirachyChanged))]
    public delegate void PartChildAttached(Part child);

    /// <summary>
    /// Child part has been detached from this part. Note: this is automatically invoked by the framework, so you don't ever need to raise the event.
    /// </summary>
    [PartMessageDelegate(typeof(PartHeirachyChanged))]
    public delegate void PartChildDetached(Part child);


    /// <summary>
    /// There has been some change to the configuration for the engine.
    /// </summary>
    [PartMessageDelegate]
    public delegate void PartEngineConfigChanged();

}
