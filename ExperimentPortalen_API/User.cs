namespace ExperimentPortalen_API
{
    public class User
    {
        //Users are created using builderpattern

        public uint id { get; set; }
        public string name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public uint role { get; set; }
       
        public User(uint id, string name, string email, string password, uint role)
        {
            this.id = id;
            this.name = name;
            this.email = email;
            this.password = password;
            this.role = role;
        }
    }
}
