namespace Buffalo
{
    public interface IAspect
    {
        void Before();
        void After();
        void Around();
        void Exception();
    }
}
