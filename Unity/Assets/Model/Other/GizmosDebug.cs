using System.Collections.Generic;
using UnityEngine;

namespace ETModel
{
    public class GizmosDebug: MonoBehaviour
    {
        public static GizmosDebug Instance { get; private set; }

        public List<Vector3> Path;

        private void Awake()
        {
            Instance = this;
            Path=new List<Vector3>();
        }

        private void OnDrawGizmos()
        {
            if (this.Path.Count < 2)
            {
                return;
            }
            for (int i = 0; i < Path.Count - 1; ++i)
            {
                Gizmos.DrawLine(Path[i], Path[i + 1]);
            }
        }
    }
}