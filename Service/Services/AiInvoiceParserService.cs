using Common.Dto;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class AiInvoiceParserService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private const int MaxRetries = 3;
        private const int DelayBetweenRequestsMs = 5000;
        public AiInvoiceParserService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"] ?? throw new Exception("Gemini API Key missing");
        }

        public async Task<createExpenseDto?> ParseInvoiceAsync(string emailText)
        {
            if (string.IsNullOrWhiteSpace(emailText)) return null;

            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            string prompt = $@"
You are a financial data extractor. Analyze the following email text and extract invoice/receipt details.

Today's date: {today}

EMAIL TEXT:
{emailText}

Rules:
- If installments are mentioned (תשלומים / payments), set InstallmentsCount accordingly
- If it's a subscription (מנוי / חיוב קבוע), set IsRecurring to true
- If date is unclear, use today's date: {today}
- If amount is unclear, use 0
- MerchantName must not be empty; use the sender or brand name
";

            var url = $"v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    responseSchema = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            MerchantName = new { type = "STRING", description = "The business name or brand name" },
                            Amount = new { type = "NUMBER", description = "The total amount paid as a number" },
                            Date = new { type = "STRING", description = "The transaction date in yyyy-MM-dd format" },
                            InstallmentsCount = new { type = "INTEGER", description = "Number of installments, 1 if not specified" },
                            InstallmentAmount = new { type = "NUMBER", description = "Amount per installment, same as Amount if 1 installment" },
                            IsRecurring = new { type = "BOOLEAN", description = "True if subscription/recurring, false otherwise" },
                            DayOfMonth = new { type = "INTEGER", description = "Day of month for recurring charge, 0 if not recurring" },
                            Description = new { type = "STRING", description = "Short description of what was purchased" }
                        },
                        required = new[] { "MerchantName", "Amount", "Date" }
                    }
                }
            };

            var jsonRequest = JsonConvert.SerializeObject(requestBody);
            // delay לפני כל בקשה כדי לא לעבור את המגבלה
            await Task.Delay(DelayBetweenRequestsMs);

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(url, content);

                    // טיפול ב-429 - המתנה וניסיון חוזר
                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        int waitSeconds = attempt * 20;
                        Console.WriteLine($"[AiParser] ⏳ Rate limited (429). Attempt {attempt}/{MaxRetries}. Waiting {waitSeconds}s...");

                        if (attempt == MaxRetries)
                        {
                            Console.WriteLine("[AiParser] ❌ Max retries reached. Skipping.");
                            return null;
                        }

                        await Task.Delay(TimeSpan.FromSeconds(waitSeconds));
                        continue;
                    }
                    var responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[AiParser] 📥 Status: {response.StatusCode}");
                    Console.WriteLine($"[AiParser] 📥 Response: {responseBody}");
                    response.EnsureSuccessStatusCode();

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    dynamic dynamicResult = JsonConvert.DeserializeObject(jsonResponse);

                    string json = dynamicResult.candidates[0].content.parts[0].text;
                    var dto = JsonConvert.DeserializeObject<createExpenseDto>(json);

                    if (dto == null)
                    {
                        Console.WriteLine("[AiParser] ⚠️ Deserialization returned null.");
                        return null;
                    }

                    if (dto.Date == default)
                        dto.Date = DateTime.UtcNow;

                    if (dto.InstallmentAmount == 0 && dto.InstallmentsCount > 1)
                        dto.InstallmentAmount = dto.Amount / dto.InstallmentsCount;

                    if (dto.InstallmentAmount == 0)
                        dto.InstallmentAmount = dto.Amount;

                    Console.WriteLine($"[AiParser] ✅ Parsed: {dto.MerchantName}, {dto.Amount:N2}₪, {dto.InstallmentsCount} installments");
                    return dto;
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"[AiParser] ❌ JSON parse error: {ex.Message}");
                    return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AiParser] ❌ Gemini error: {ex.Message}");
                    return null;
                }
            }

            return null;
        }
        public async Task<createExpenseDto?> ParseInvoicePdfAsync(byte[] pdfBytes)
        {
            string base64Pdf = Convert.ToBase64String(pdfBytes);
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            var url = $"v1beta/models/gemini-2.5-flash-lite:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
            new
            {
                parts = new object[]
                {
                    new
                    {
                        inline_data = new
                        {
                            mime_type = "application/pdf",
                            data = base64Pdf
                        }
                    },
                    new
                    {
                        text = $@"You are a financial data extractor. Extract invoice/receipt details from this PDF.
Today's date: {today}
Rules:
- If installments mentioned (תשלומים/payments), set InstallmentsCount accordingly
- If subscription (מנוי/חיוב קבוע), set IsRecurring to true
- If date unclear, use today: {today}
- If amount unclear, use 0
- MerchantName must not be empty"
                    }
                }
            }
        },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    responseSchema = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            MerchantName = new { type = "STRING", description = "The business name or brand name" },
                            Amount = new { type = "NUMBER", description = "The total amount paid as a number" },
                            Date = new { type = "STRING", description = "The transaction date in yyyy-MM-dd format" },
                            InstallmentsCount = new { type = "INTEGER", description = "Number of installments, 1 if not specified" },
                            InstallmentAmount = new { type = "NUMBER", description = "Amount per installment, same as Amount if 1 installment" },
                            IsRecurring = new { type = "BOOLEAN", description = "True if subscription/recurring, false otherwise" },
                            DayOfMonth = new { type = "INTEGER", description = "Day of month for recurring charge, 0 if not recurring" },
                            Description = new { type = "STRING", description = "Short description of what was purchased" }
                        },
                        required = new[] { "MerchantName", "Amount", "Date" }
                    }
                }
            };

            await Task.Delay(DelayBetweenRequestsMs);

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    var jsonRequest = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(url, content);

                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        int waitSeconds = (int)Math.Pow(2, attempt);
                        Console.WriteLine($"[AiParser] ⏳ Rate limited (429). Attempt {attempt}/{MaxRetries}. Waiting {waitSeconds}s...");
                        if (attempt == MaxRetries) return null;
                        await Task.Delay(TimeSpan.FromSeconds(waitSeconds));
                        continue;
                    }

                    var responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[AiParser PDF] 📥 Status: {response.StatusCode}");
                    Console.WriteLine($"[AiParser PDF] 📥 Response: {responseBody}");
                    response.EnsureSuccessStatusCode();

                    dynamic dynamicResult = JsonConvert.DeserializeObject(responseBody);
                    string json = dynamicResult.candidates[0].content.parts[0].text;
                    var dto = JsonConvert.DeserializeObject<createExpenseDto>(json);

                    if (dto == null) return null;
                    if (dto.Date == default) dto.Date = DateTime.UtcNow;
                    if (dto.InstallmentAmount == 0 && dto.InstallmentsCount > 1)
                        dto.InstallmentAmount = dto.Amount / dto.InstallmentsCount;
                    if (dto.InstallmentAmount == 0)
                        dto.InstallmentAmount = dto.Amount;

                    Console.WriteLine($"[AiParser PDF] ✅ Parsed: {dto.MerchantName}, {dto.Amount:N2}₪, {dto.InstallmentsCount} installments");
                    return dto;
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"[AiParser PDF] ❌ JSON parse error: {ex.Message}");
                    return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AiParser PDF] ❌ Gemini error: {ex.Message}");
                    return null;
                }
            }

            return null;
        }
    }
}