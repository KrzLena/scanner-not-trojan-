using System.Text.Json.Serialization;

namespace MauiApp1;

public class PersonRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string FirstName { get; set; } = "";

    public string LastName { get; set; } = "";

    public string QrCode { get; set; } = "";

    [JsonIgnore]
    public bool IsSelected { get; set; }

    [JsonIgnore]
    public string FullName => FirstName + " " + LastName;
}