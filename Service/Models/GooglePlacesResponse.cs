namespace Service.Models
{
    public class GooglePlacesResponse
    {
        public List<PlaceResult>? Places { get; set; }
    }

    public class PlaceResult
    {
        public DisplayName? DisplayName { get; set; }
        public List<string>? Types { get; set; }
    }

    public class DisplayName
    {
        public string? Text { get; set; }
    }
}