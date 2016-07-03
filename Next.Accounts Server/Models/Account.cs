using Next.Accounts_Server.Application_Space;

namespace Next.Accounts_Server.Models
{
    public class Account
    {
        public int Id { get; set; }

        public string Login { get; set; } = null;

        public string Password { get; set; } = null;

        public bool Available { get; set; } = true;

        public string ComputerName { get; set; } = null;

        public Account(int id, string login, string pass)
        {
            Id = id;
            Login = login;
            Password = pass;
        }
    }
}