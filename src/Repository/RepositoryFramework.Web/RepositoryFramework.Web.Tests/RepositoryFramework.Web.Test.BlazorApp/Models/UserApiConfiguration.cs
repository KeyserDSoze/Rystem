using System.Text.Json.Serialization;

namespace Whistleblowing.Licensing.Models
{
    public class UserApiConfiguration
    {
        [JsonPropertyName("createRole")]
        public required ApiConfiguration CreateRole { get; set; }
        [JsonPropertyName("createRoleForBusiness")]
        public required ApiConfiguration CreateRoleForBusiness { get; set; }
        [JsonPropertyName("updateRole")]
        public required ApiConfiguration UpdateRole { get; set; }
        [JsonPropertyName("deleteRole")]
        public required ApiConfiguration DeleteRole { get; set; }
        [JsonPropertyName("setRole")]
        public required ApiConfiguration SetRole { get; set; }
        [JsonPropertyName("removeRole")]
        public required ApiConfiguration RemoveRole { get; set; }
        [JsonPropertyName("createNewUser")]
        public required ApiConfiguration CreateNewUser { get; set; }
        [JsonPropertyName("allUserByRole")]
        public required ApiConfiguration AllUserByRole { get; set; }
        [JsonPropertyName("pageUsers")]
        public required ApiConfiguration PageUsers { get; set; }
        [JsonPropertyName("allRoles")]
        public required ApiConfiguration AllRoles { get; set; }
        [JsonPropertyName("updateUser")]
        public required ApiConfiguration UpdateUser { get; set; }
    }

}
