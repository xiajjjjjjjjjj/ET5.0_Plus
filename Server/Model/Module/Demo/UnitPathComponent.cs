using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ETModel
{
    public class UnitPathComponent: Component
    {
        public Vector3 Target;

        public List<Vector3> Path=new List<Vector3>();

        public CancellationTokenSource CancellationTokenSource;
        

        public override void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }
            base.Dispose();
            
            Path.Clear();
        }
    }
}