using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Repository.Entities;
using Repository.Interfaces;
using Service.Interfaces;
using Service.Models;
using Common.Dto;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace Service.Services
{
    public class GmailInvoiceService
    {
        private readonly IExpenseService _expenseService;
        private readonly IRepository<Expense> _expenseRepository;
        private readonly AiInvoiceParserService _aiParser;
        private readonly IReceiptParserService _receiptParser;
        private readonly IConfiguration _config;

        public GmailInvoiceService(
            IExpenseService expenseService,
            IRepository<Expense> expenseRepository,
            AiInvoiceParserService aiParser,
            IReceiptParserService receiptParser,
            IConfiguration config)
        {
            _expenseService = expenseService;
            _expenseRepository = expenseRepository;
            _aiParser = aiParser;
            _receiptParser = receiptParser;
            _config = config;
        }

        public async Task<int> ProcessUserInvoicesAsync(string refreshToken, int userId)
        {
            Console.WriteLine($"[Gmail] Starting invoice scan for user {userId}");

            var flow = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = _config["Authentication:Google:ClientId"] ?? "",
                        ClientSecret = _config["Authentication:Google:ClientSecret"] ?? ""
                    },
                    Scopes = new[] { GmailService.Scope.GmailReadonly }
                });

            var newToken = await flow.RefreshTokenAsync("", refreshToken, CancellationToken.None);

            if (string.IsNullOrEmpty(newToken?.AccessToken))
            {
                Console.WriteLine("[Gmail] ❌ Failed to refresh Access Token.");
                return 0;
            }

            var credential = GoogleCredential.FromAccessToken(newToken.AccessToken);
            var gmailService = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Smart Finance"
            });

            string twoWeeksAgo = DateTime.UtcNow.AddDays(-14).ToString("yyyy/MM/dd");
            var listRequest = gmailService.Users.Messages.List("me");
            listRequest.Q = $"(חשבונית OR קבלה OR invoice OR receipt OR \"אישור תשלום\" OR \"order confirmation\") after:{twoWeeksAgo}";
            listRequest.MaxResults = 2;

            var listResponse = await listRequest.ExecuteAsync();

            if (listResponse.Messages == null || !listResponse.Messages.Any())
            {
                Console.WriteLine("[Gmail] No relevant emails found.");
                return 0;
            }

            Console.WriteLine($"[Gmail] Found {listResponse.Messages.Count} messages matching filter");

            var googleMessageIds = listResponse.Messages.Select(m => m.Id).ToList();

            var existingExpenses = await _expenseRepository.Find(e => e.GmailMessageId != null && googleMessageIds.Contains(e.GmailMessageId));
            var existingMessageIds = new HashSet<string>(existingExpenses.Select(e => e.GmailMessageId).Where(id => id != null)!);
            int processedCount = 0;

            foreach (var messageSummary in listResponse.Messages)
            {
                try
                {
                    if (existingMessageIds.Contains(messageSummary.Id))
                    {
                        Console.WriteLine($"[Gmail] Skipping duplicate: {messageSummary.Id}");
                        continue;
                    }

                    var message = await gmailService.Users.Messages.Get("me", messageSummary.Id).ExecuteAsync();

                    // ── חדש: שמור PDF bytes אם יש ──
                    byte[]? pdfBytes = null;
                    var pdfPartForBytes = FindPdfPart(message.Payload?.Parts ?? new List<Google.Apis.Gmail.v1.Data.MessagePart>());
                    if (pdfPartForBytes?.Body?.AttachmentId != null)
                        pdfBytes = await GetAttachmentBytesRawAsync(message.Id, pdfPartForBytes.Body.AttachmentId, gmailService);
                    // ─────────────────────────────────

                    string emailBody = await ExtractEmailBodyAsync(message, gmailService);

                    bool hasPdf = pdfBytes != null;
                    if (!IsActualInvoice(emailBody, message.Snippet) && !hasPdf)
                    {
                        Console.WriteLine("[Gmail] Not an invoice, skipping");
                        continue;
                    }

                    Console.WriteLine($"[Gmail] Processing: {message.Snippet?[..Math.Min(60, message.Snippet?.Length ?? 0)]}");

                    createExpenseDto? parsedDto = null;

                    // שלב 1: נסה Regex קודם — חינם ומהיר
                    //var receiptResult = await _receiptParser.ParseReceiptText(emailBody);
                    //if (receiptResult.IsValid)
                    //{
                    //    Console.WriteLine($"[Gmail] ✅ Parsed by Regex: {receiptResult.MerchantName} {receiptResult.TotalAmount:N2}₪");
                    //    parsedDto = MapReceiptToDto(receiptResult);
                    //}
                    //else
                    //{
                    //    // שלב 2: Regex נכשל → קרא ל-Gemini
                    //    Console.WriteLine($"[Gmail] Regex failed ({string.Join(", ", receiptResult.Errors)}), trying Gemini...");
                    //    await Task.Delay(4000);
                    //    parsedDto = await _aiParser.ParseInvoiceAsync(emailBody);
                    //}
                    //if ((parsedDto == null || parsedDto.Amount == 0) && pdfBytes != null)
                    //{
                    //    Console.WriteLine("[Gmail] 📤 Trying Gemini with raw PDF...");
                    //    parsedDto = await _aiParser.ParseInvoicePdfAsync(pdfBytes);
                    //}
                    var receiptResult = await _receiptParser.ParseReceiptText(emailBody);
                    if (receiptResult.IsValid)
                    {
                        Console.WriteLine($"[Gmail] ✅ Parsed by Regex: {receiptResult.MerchantName} {receiptResult.TotalAmount:N2}₪");
                        parsedDto = MapReceiptToDto(receiptResult);
                    }
                    else if (pdfBytes != null && string.IsNullOrWhiteSpace(emailBody.Trim().Replace("תודה שקניתם אצלינו", "").Trim()))
                    {
                        // הטקסט ריק/חסר משמעות ויש PDF — קפוץ ישירות ל-PDF
                        Console.WriteLine("[Gmail] 📤 Empty body + PDF exists, going straight to Gemini PDF...");
                        parsedDto = await _aiParser.ParseInvoicePdfAsync(pdfBytes);
                    }
                    else
                    {
                        Console.WriteLine($"[Gmail] Regex failed, trying Gemini text...");
                        await Task.Delay(4000);
                        parsedDto = await _aiParser.ParseInvoiceAsync(emailBody);

                        if ((parsedDto == null || parsedDto.Amount == 0) && pdfBytes != null)
                        {
                            Console.WriteLine("[Gmail] 📤 Trying Gemini with raw PDF...");
                            parsedDto = await _aiParser.ParseInvoicePdfAsync(pdfBytes);
                        }
                    }

                    if (parsedDto == null || parsedDto.Amount == 0)
                    {
                        Console.WriteLine("[Gmail] ⚠️ Skipping – no amount");
                        continue;
                    }

                    await _expenseService.AddExpense(parsedDto, userId, messageSummary.Id);
                    processedCount++;
                    Console.WriteLine($"[Gmail] ✅ Saved: {parsedDto.MerchantName} {parsedDto.Amount:N2}₪");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Gmail] ❌ Error: {ex.Message}");
                }
            }

            Console.WriteLine($"[Gmail] Done. Processed {processedCount} new invoices.");
            return processedCount;
        }

        private async Task<byte[]?> GetAttachmentBytesRawAsync(string messageId, string attachmentId, GmailService gmailService)
        {
            try
            {
                var attachment = await gmailService.Users.Messages.Attachments.Get("me", messageId, attachmentId).ExecuteAsync();
                if (attachment?.Data == null) return null;

                var base64 = attachment.Data.Replace("-", "+").Replace("_", "/");
                switch (base64.Length % 4)
                {
                    case 2: base64 += "=="; break;
                    case 3: base64 += "="; break;
                }
                return Convert.FromBase64String(base64);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gmail] ❌ Failed to get attachment bytes: {ex.Message}");
                return null;
            }
        }

        // ──────────────────────────────────────────────
        // Mapping: ParsedReceipt → createExpenseDto
        // ──────────────────────────────────────────────
        private createExpenseDto MapReceiptToDto(ParsedReceipt receipt)
        {
            return new createExpenseDto
            {
                MerchantName = receipt.MerchantName,
                Amount = receipt.TotalAmount,
                Description = "נטען אוטומטית מ-Gmail",
                Date = receipt.TransactionDate,
                CategoryId = 0,
                InstallmentsCount = receipt.InstallmentsCount > 0 ? receipt.InstallmentsCount : 1,
                InstallmentAmount = receipt.InstallmentAmount > 0
                    ? receipt.InstallmentAmount
                    : receipt.TotalAmount,
                FirstInstallmentDate = receipt.HasInstallments ? receipt.TransactionDate : null,
                IsRecurring = receipt.IsRecurring,
                DayOfMonth = receipt.IsRecurring ? receipt.TransactionDate.Day : null,
                StartDate = receipt.IsRecurring || receipt.HasInstallments ? receipt.TransactionDate : null,
                EndDate = receipt.HasInstallments && receipt.InstallmentsCount > 1
                    ? receipt.TransactionDate.AddMonths(receipt.InstallmentsCount - 1)
                    : null
            };
        }

        // ──────────────────────────────────────────────
        // חילוץ תוכן המייל — סדר עדיפויות חדש
        // ──────────────────────────────────────────────
        private async Task<string> ExtractEmailBodyAsync(
            Google.Apis.Gmail.v1.Data.Message message,
            GmailService gmailService)
        {
            if (message.Payload?.Parts != null)
            {
                // ✅ שלב 1: PDF ראשון — הכי חשוב לחשבוניות
                var pdfPart = FindPdfPart(message.Payload.Parts);
                if (pdfPart?.Body?.AttachmentId != null)
                {
                    Console.WriteLine("[Gmail] 📎 Found PDF attachment, extracting...");
                    var pdfText = await TryExtractPdfAsync(message.Id, pdfPart.Body.AttachmentId, gmailService);
                    if (!string.IsNullOrWhiteSpace(pdfText))
                    {
                        Console.WriteLine($"[Gmail] ✅ Using PDF content ({pdfText.Length} chars)");
                        return pdfText;
                    }
                    Console.WriteLine("[Gmail] ⚠️ PDF extraction failed, falling back to text");
                }

                // ✅ שלב 2: טקסט רגיל
                var plainPart = message.Payload.Parts.FirstOrDefault(p => p.MimeType == "text/plain");
                if (plainPart?.Body?.Data != null)
                {
                    Console.WriteLine("[Gmail] 📄 Using text/plain");
                    return DecodeBase64Url(plainPart.Body.Data);
                }

                // ✅ שלב 3: HTML
                var htmlPart = message.Payload.Parts.FirstOrDefault(p => p.MimeType == "text/html");
                if (htmlPart?.Body?.Data != null)
                {
                    Console.WriteLine("[Gmail] 🌐 Using text/html");
                    return DecodeBase64Url(htmlPart.Body.Data);
                }

                // ✅ שלב 4: חיפוש בחלקים מקוננים (multipart/alternative)
                foreach (var part in message.Payload.Parts)
                {
                    if (part.Parts == null) continue;

                    // PDF מקונן
                    var nestedPdf = FindPdfPart(part.Parts);
                    if (nestedPdf?.Body?.AttachmentId != null)
                    {
                        Console.WriteLine("[Gmail] 📎 Found nested PDF");
                        var pdfText = await TryExtractPdfAsync(message.Id, nestedPdf.Body.AttachmentId, gmailService);
                        if (!string.IsNullOrWhiteSpace(pdfText))
                        {
                            Console.WriteLine($"[Gmail] ✅ Using nested PDF content ({pdfText.Length} chars)");
                            return pdfText;
                        }
                    }

                    // טקסט מקונן
                    var nestedPlain = part.Parts.FirstOrDefault(p => p.MimeType == "text/plain");
                    if (nestedPlain?.Body?.Data != null)
                    {
                        Console.WriteLine("[Gmail] 📄 Using nested text/plain");
                        return DecodeBase64Url(nestedPlain.Body.Data);
                    }

                    var nestedHtml = part.Parts.FirstOrDefault(p => p.MimeType == "text/html");
                    if (nestedHtml?.Body?.Data != null)
                    {
                        Console.WriteLine("[Gmail] 🌐 Using nested text/html");
                        return DecodeBase64Url(nestedHtml.Body.Data);
                    }
                }
            }

            // ✅ שלב 5: גוף ישיר (מיילים פשוטים ללא parts)
            if (message.Payload?.Body?.Data != null)
            {
                Console.WriteLine("[Gmail] 📄 Using direct body");
                return DecodeBase64Url(message.Payload.Body.Data);
            }

            // ✅ שלב 6: fallback לsnippet
            Console.WriteLine("[Gmail] ⚠️ Using snippet as fallback");
            return message.Snippet ?? "";
        }

        // ──────────────────────────────────────────────
        // מוצא PDF בתוך רשימת parts
        // ──────────────────────────────────────────────
        private Google.Apis.Gmail.v1.Data.MessagePart? FindPdfPart(
            IList<Google.Apis.Gmail.v1.Data.MessagePart> parts)
        {
            return parts.FirstOrDefault(p =>
                p.MimeType == "application/pdf" &&
                p.Body?.AttachmentId != null);
        }

        // ──────────────────────────────────────────────
        // הורדת PDF מ-Gmail וחילוץ טקסט
        // ──────────────────────────────────────────────
        private async Task<string> TryExtractPdfAsync(
            string messageId,
            string attachmentId,
            GmailService gmailService)
        {
            try
            {
                var attachment = await gmailService.Users.Messages.Attachments
                    .Get("me", messageId, attachmentId)
                    .ExecuteAsync();

                if (attachment?.Data == null) return "";

                var base64 = attachment.Data.Replace("-", "+").Replace("_", "/");
                switch (base64.Length % 4)
                {
                    case 2: base64 += "=="; break;
                    case 3: base64 += "="; break;
                }

                var pdfBytes = Convert.FromBase64String(base64);
                return ExtractTextFromPdf(pdfBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gmail] PDF read error: {ex.Message}");
                return "";
            }
        }

        // ──────────────────────────────────────────────
        // חילוץ טקסט מ-PDF עם PdfPig
        // ──────────────────────────────────────────────
        private string ExtractTextFromPdf(byte[] pdfBytes)
        {
            try
            {
                using var document = PdfDocument.Open(pdfBytes);
                var sb = new StringBuilder();
                foreach (var page in document.GetPages())
                    sb.AppendLine(string.Join(" ", page.GetWords()));

                var result = sb.ToString().Trim();

                // בדוק אם הטקסט "זבל" — יותר מ-40% תווים לא-ASCII
                int nonAscii = result.Count(c => c > 127);
                if (result.Length > 0 && (double)nonAscii / result.Length > 0.4)
                {
                    Console.WriteLine("[Gmail] ⚠️ PDF text looks corrupted (garbled encoding), skipping PDF");
                    return "";
                }

                Console.WriteLine($"[Gmail] PDF extracted {result.Length} chars");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gmail] PDF parse error: {ex.Message}");
                return "";
            }
        }

        // ──────────────────────────────────────────────
        // Base64 decode
        // ──────────────────────────────────────────────
        private static string DecodeBase64Url(string base64Url)
        {
            var base64 = base64Url.Replace("-", "+").Replace("_", "/");
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }

        // ──────────────────────────────────────────────
        // בדיקה אם המייל הוא חשבונית אמיתית
        // ──────────────────────────────────────────────
        private static bool IsActualInvoice(string body, string? snippet)
        {
            var content = (body + " " + (snippet ?? "")).ToLower();

            bool hasActualAmount = Regex.IsMatch(content, @"[\d,]+\.?\d*\s*₪") ||
                       Regex.IsMatch(content, @"₪\s*[\d,]+") ||
                       Regex.IsMatch(content, @"total[:\s]+\$?[\d,]+\.?\d*") ||
                       Regex.IsMatch(content, @"\b\d+\.\d{2}\b") ||      
                       Regex.IsMatch(content, @"amount[:\s]+[\d,]+") ||  
                       content.Contains("לתשלום") ||
                       content.Contains("סה\"כ") ||                       
                       content.Contains("payment") ||                    
                       content.Contains("charged") ||
                       content.Contains("order total");
                      

            string[] spamKeywords = {
        "newsletter", "unsubscribe", "הסר מרשימה",
        "ניוזלטר", "הצטרפות לרשימה", "הסרה מרשימת התפוצה"
    };

            string[] socialKeywords = {
                 "הטיפ השבועי",
                "donate", "תרומה", "unsubscribe"
            };

            bool isSpam = spamKeywords.Any(k => content.Contains(k)) ||
                          socialKeywords.Any(k => content.Contains(k));

            return hasActualAmount && !isSpam;
        }
    }
}