namespace ExperimentPortalen_API
{
    //Builder pattern for user
    internal class UserBuilder
    {
        private uint id;
        private string name;
        private string email;
        private string password;
        private uint role;

        public UserBuilder Id(uint id)
        {
            this.id = id;
            return this;
        }

        public UserBuilder Name(string name)
        {
            this.name = name;
            return this;
        }

        public UserBuilder Email(string email)
        {
            this.email = email;
            return this;
        }

        public UserBuilder Password(string password)
        {
            this.password = password;
            return this;
        }

        public UserBuilder Role(uint role)
        {
            this.role = role;
            return this;
        }
    }
}
