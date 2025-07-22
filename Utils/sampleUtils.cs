
using WEBAPI_m1IL_1.Models;

namespace WEBAPI_m1IL_1.Utils
{
    public static class SampleUtils
{
    public static string GenerateUUID()
    {
            Guid myuuid = Guid.NewGuid();
            return  myuuid.ToString();
    }
}

}