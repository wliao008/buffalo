
namespace Buffalo
{
    public sealed class MethodArgs
    {
        private string name;
        private string fullName;

        public string Name
        {
            get
            {
                return this.name;
            }
        }

        public string FullName
        {
            get
            {
                return this.fullName;
            }
        }

        public void setNames(string name, string fullname)
        {
            this.name = name;
            this.fullName = fullname;
        }
    }
}
