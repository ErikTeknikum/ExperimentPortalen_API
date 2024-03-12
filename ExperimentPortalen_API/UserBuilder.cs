namespace ExperimentPortalen_API
{
    //Users are created using builderpattern to fullfill project requirements

    public class UserBuilder
    {
        private uint id;
        private string name = String.Empty;
        private string email = String.Empty;
        private string password = String.Empty;
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

        public User Build()
        {
            return new User(id, name, email, password, role);
        }
    }
}
