using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Xeek.ToolsAndExtensions
{
    public static class TransformExtensions
    {
        public static Transform FindRecursively(this Transform parent, string n, bool ignoreCase = false)
        {
            return parent.GetAllChildren(n, ignoreCase).FirstOrDefault();
        }

        public static IEnumerable<Transform> GetAllChildren(this Transform parent, string n, bool ignoreCase = false)
        {
            return parent.GetAllChildren().Where(transform => string.Compare(transform.name, n, ignoreCase) == 0);
        }

        public static IEnumerable<Transform> GetAllChildren(this Transform parent)
        {
            foreach(Transform child in parent)
            {
                yield return child;

                foreach(Transform grandChild in child.GetAllChildren())
                {
                    yield return grandChild;
                }
            }
        }
    }
}
