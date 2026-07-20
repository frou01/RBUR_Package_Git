using UdonSharp;
using VRC.Udon.Common;

namespace frou01.RigidBodyTrain
{
    public class AbstractPointSetter : UdonSharpBehaviour
    {
        public UdonSharpBehaviour[] callbackUdons = new UdonSharpBehaviour[0];
        void Start()
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
        public void SyncEvent()
        {
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            applyChange();
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            if (!result.success)
            {
                SendCustomEventDelayedSeconds(nameof(SyncEvent), UnityEngine.Random.Range(1, 4f));
            }
        }
        public virtual Rail_Script get_current_To()
        {
            return null;
        }

        public virtual Rail_Script[] getRoutes()
        {
            return null;
        }
        public virtual int get_current_To_Index()
        {
            return -1;
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public virtual void DrawGizmo_From()
        {
        }
        public virtual void DrawGizmo_To(Rail_Script targetRail)
        {

        }
#endif
    }
}