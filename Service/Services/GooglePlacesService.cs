using Common.Enum;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Repository.Entities;
using Repository.Repositories;
using Service.Interfaces;
using Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class GooglePlacesService : IGooglePlacesService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GooglePlacesService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["Google:ApiKey"]
                ?? throw new Exception("Google API Key is missing");
        }

        public async Task<List<string>> GetPlaceTypesAsync(string businessName)
        {
            var url = "https://places.googleapis.com/v1/places:searchText";

            var requestBody = new { textQuery = businessName };

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(requestBody)
            };

            request.Headers.Add("X-Goog-Api-Key", _apiKey);
            request.Headers.Add("X-Goog-FieldMask", "places.types,places.displayName");

            var response = await _httpClient.SendAsync(request);
            var debugContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Google Response: {debugContent}");
            if (!response.IsSuccessStatusCode)
                return new List<string>();

            var data = await response.Content.ReadFromJsonAsync<GooglePlacesResponse>();

            if (data?.Places == null || data.Places.Count == 0)
                return new List<string>();

            var bestMatch = data.Places
                .OrderByDescending(p => Similarity(p.DisplayName?.Text, businessName))
                .First();

            return bestMatch.Types ?? new List<string>();
        }
        
        private int Similarity(string? a, string b)
        {
            if (string.IsNullOrEmpty(a)) return 0;

            a = a.ToLower();
            b = b.ToLower();

            return b.Split(' ')
                .Count(word => a.Contains(word));
        }

    }
}
