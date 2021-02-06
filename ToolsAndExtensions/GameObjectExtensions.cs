using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Xeek.ToolsAndExtensions
{
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Try to get the bounds of the GameObject first using all colliders encapsulated then by using the first 
        /// child SkinnedMeshRenderer. Bounds are in local coordinates.
        /// </summary>
        /// <remarks>
        /// It's not recommended to use this on a frame-by-frame basis within Update, LateUpdate, FixedUpdate, etc.
        /// </remarks>
        /// <param name="gameObject">The GameObject to find Collider or SkinnedMeshRenderer child components</param>
        /// <param name="bounds">The resulting bounds which is empty on failure</param>
        /// <param name="ignoreColliders">Skip trying to get bounds based on child Colliders an duse the first child 
        /// SkinnedMeshRenderer instead.</param>
        /// <returns><strong>true</strong> if the bounds was successfully calculated.</returns>
        public static bool GetBounds(this GameObject gameObject, out Bounds bounds, bool ignoreColliders = false)
        {
            if (!ignoreColliders && GetBoundsFromColliders(gameObject, out bounds))
            {
                return true;
            }
            else
            {
                return GetBoundsFromSkinnedMeshRenderers(gameObject, out bounds);
            }
        }

        /// <summary>
        /// Try to get the bounds of the GameObject using all child colliders encapsulated. 
        /// <strong>Bounds are in local coordinates.</strong>
        /// </summary>
        /// <remarks>
        /// It's not recommended to use this on a frame-by-frame basis within Update, LateUpdate, FixedUpdate, etc.
        /// </remarks>
        /// <param name="gameObject">The GameObject to find Collider or SkinnedMeshRenderer child components</param>
        /// <param name="bounds">The resulting bounds, empty on failure</param>
        /// <param name="onlyEnabled">Ignore disabled colliders</param>
        /// <returns><strong>true</strong> if the bounds was successfully calculated.</returns>
        public static bool GetBoundsFromColliders(this GameObject gameObject, out Bounds bounds, bool onlyEnabled = true)
        {
            var colliders = gameObject.GetComponentsInChildren<Collider>().Where(x => !onlyEnabled || x.enabled);
            if (colliders.Any())
            {
                var boundsResult = colliders.First().bounds;
                colliders.Skip(1).ToList().ForEach(x => boundsResult.Encapsulate(x.bounds));

                // The bounds is in world coordindates, convert to local coordinates:
                // center is the only coordinate point (not a size)
                boundsResult.center = gameObject.transform.InverseTransformPoint(boundsResult.center);

                bounds = boundsResult;
                return true;
            }

            bounds = new Bounds();
            return false;
        }

        /// <summary>
        /// Try to get the bounds of the GameObject using child SkinnedMeshRenderers. 
        /// <strong>Bounds are in local coordinates.</strong>
        /// </summary>
        /// <remarks>
        /// It's not recommended to use this on a frame-by-frame basis within Update, LateUpdate, FixedUpdate, etc.
        /// </remarks>
        /// <param name="gameObject">The GameObject to find Collider or SkinnedMeshRenderer child components</param>
        /// <param name="bounds">The resulting bounds, empty on failure</param>
        /// <param name="onlyEnabled">Ignore disabled mesh renderers</param>
        /// <returns><strong>true</strong> if the bounds was successfully calculated.</returns>
        public static bool GetBoundsFromSkinnedMeshRenderers(this GameObject gameObject, out Bounds bounds, bool onlyEnabled = true)
        {
            var skinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>().Where(x => !onlyEnabled || x.enabled);
            if (skinnedMeshRenderers.Any())
            {
                var boundsResult = skinnedMeshRenderers.First().bounds;
                skinnedMeshRenderers.Skip(1).ToList().ForEach(x => boundsResult.Encapsulate(x.bounds));

                // The bounds is in world coordindates, convert to local coordinates:
                // center is the only coordinate point (not a size)
                boundsResult.center = gameObject.transform.InverseTransformPoint(boundsResult.center);

                bounds = boundsResult;
                return true;
            }

            bounds = new Bounds();
            return false;
        }
    }
}
