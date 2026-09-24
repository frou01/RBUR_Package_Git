using UdonSharp;
using UnityEngine;
using VRC.Udon.Common;

namespace frou01.RigidBodyTrain
{
    public

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        abstract
#endif
        class AbstractPointSetter : UdonSharpBehaviour
    {
        public UdonSharpBehaviour[] callbackUdons = new UdonSharpBehaviour[0];
        protected virtual void Start()
        {
            applyChange();
        }
        protected virtual void applyChange()
        {
            foreach (UdonSharpBehaviour udon in callbackUdons)
            {
                udon.SendCustomEvent("PointUpdate");
            }
        }
        public virtual void set_route_To(Rail_Script setRoute)
        {
            if (!setRoute)
            {
                set_route_To(-1);
                return;
            }
            int idx = 0;
            foreach(Rail_Script route in getRoutes())
            {
                if(route == setRoute)
                {
                    set_route_To(idx);
                    break;
                }
                idx++;
            }
            return;
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public abstract void set_route_To(int routeIndex);
        public abstract Rail_Script[] getRoutes();
        public abstract int get_current_To_Index();

        public virtual void DrawGizmo_From()
        {
        }
        public virtual void DrawGizmo_To(Rail_Script targetRail)
        {

        }
        public abstract void Gizmo_LineTarget(Rail_Script targetRail,out Vector3 lineStart,out Vector3 lineEnd);
#else
        public virtual void set_route_To(int routeIndex)
        {
            return;
        }
        public virtual Rail_Script[] getRoutes()
        {
            return null;
        }
        public virtual int get_current_To_Index()
        {
            return -1;//-1 means null
        }
#endif
    }
}