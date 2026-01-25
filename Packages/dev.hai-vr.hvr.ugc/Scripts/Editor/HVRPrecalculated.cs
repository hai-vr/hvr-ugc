using System.Linq;
using UnityEngine;

namespace HVR.UGC.Editor
{
    public static class HVRPrecalculated
    {
        public static void PrecalculateCollisionMeshes(GameObject copy)
        {
            var ugcAvatar = copy.GetComponent<HVRUGCAvatar>();

            var collisionMeshes = ugcAvatar.collisionMeshes.Where(renderer => renderer != null).ToArray();
            if (collisionMeshes.Length > 0)
            {
                // TODO: This does nothing for now
                Remesher_Precalculate(collisionMeshes);
            }
        }

        public static void Remesher_Precalculate(SkinnedMeshRenderer[] collisionMeshes)
        {
            // TODO: Calculate the colliders in advance so that it doesn't hitch when the avatar loads.
        }
    }
}