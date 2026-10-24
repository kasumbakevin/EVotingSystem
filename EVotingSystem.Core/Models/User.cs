namespace EVotingSystem.Models
{
    public abstract class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool MustChangePassword { get; set; }
        public bool IsActive { get; set; } = true;

        protected User() { }

        protected User(int id, string name, string username, string passwordHash)
        {
            Id = id;
            Name = name;
            Username = username;
            PasswordHash = passwordHash;
        }

        public abstract void DisplayRole();
    }
}
