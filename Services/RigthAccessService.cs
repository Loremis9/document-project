namespace WEBAPI_m1IL_1.Services
{
    public class RigthAccessService
    {
        public bool HasAccess(string userId, string resourceId)
        {
            // Logique pour vérifier si l'utilisateur a accès à la ressource
            // Par exemple, vérifier dans une base de données ou un service externe
            // Pour l'instant, on retourne toujours true pour simuler un accès autorisé
            return true;
        }

        public void ModifyAccessToGroup()
        {

        }
    }
}
