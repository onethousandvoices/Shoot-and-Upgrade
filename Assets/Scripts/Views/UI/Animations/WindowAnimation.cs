namespace Views.UI.Animations
{
    public abstract class WindowAnimation : MonoBehaviour
    {
        public virtual void PrepareShow() { }
        public virtual void PrepareHide() { }
        public virtual void CompleteShow() { }
        public abstract void AddShow(Sequence sequence);
        public virtual void AddHide(Sequence sequence) { }
        public abstract void ResetImmediate();
    }
}