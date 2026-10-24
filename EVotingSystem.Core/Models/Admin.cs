namespace EVotingSystem.Models
{
    public class Admin : User
    {
        public Admin() { }

        public Admin(int id, string name, string username, string passwordHash)
            : base(id, name, username, passwordHash)
        {
            MustChangePassword = true;
        }

        public override void DisplayRole()
        {
            Console.WriteLine("Role: Administrator");
        }
    }
}
