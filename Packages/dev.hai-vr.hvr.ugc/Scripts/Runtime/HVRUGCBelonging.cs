using UnityEngine;

namespace HVR.UGC
{
    /// Represents an item qualifying as a personal belonging, tied to a person. Like a smartphone or house keys, you never leave
    /// personal belongings behind at a restaurant; Belongings disappear when the person leaves.<br/>
    /// This explicitly distinguishes personal belongings from other kinds of objects that can be left behind, such as a post-it note,
    /// a photo, a business card, or money.<br/><br/>
    /// A personal belonging can sometimes be thought as something that could be an integral part of an avatar but which can be
    /// separated from it. It is probably *not* suitable for costumes or individual pieces of clothing.
    [HelpURL("https://docs.hai-vr.dev/docs/hvr/ugc/belonging")]
    [AddComponentMenu("HVR/UGC/HVRUGC Belonging")]
    public class HVRUGCBelonging : MonoBehaviour
    {
        public string belongingName;

        public bool bypassRestrictions; // For debug purposes. If true, this does not remove components from the object.
    }
}