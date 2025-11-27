
using UdonSharp;


namespace frou01.RigidBodyTrain
{
    public class TrainConnectionReciever : UdonSharpBehaviour
    {
        public string[] connectionTags;
        public virtual void TrainConnectionUpdate(Train connectedTrain, bool F_B)
        {

        }
    }
}
