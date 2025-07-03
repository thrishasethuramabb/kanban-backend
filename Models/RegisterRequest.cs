namespace kanbanBackend.Models
{
    public class RegisterRequest
    {
        public int EmpId { get; set; }
        public string Fname { get; set; }
        public string Lname { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string Role { get; set; }
    }
    public class ResetPasswordRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }

    public class ResetRoleRequest
    {
        public string Username { get; set; }
        public string Role { get; set; }
    }

}
