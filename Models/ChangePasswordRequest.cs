namespace PROJFACILITY.IA.Models
{
    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; } // Senha atual (segurança)
        public string NewPassword { get; set; } // Nova senha
    }
}