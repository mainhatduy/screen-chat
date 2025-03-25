using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ScreenOCR
{
    public class GeminiApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _apiKey;
        private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-exp-image-generation:generateContent";
        public const string DEFAULT_PROMPT = "You are a powerful and precise OCR (Optical Character Recognition) tool capable of extracting all text from provided images or documents accurately, supporting all languages including Latin alphabets, ideographic scripts (Chinese, Japanese, Korean), special character-based languages, and handwritten text. \nYour task is to precisely, fully, and clearly extract all text from this image, ensuring no words or characters are omitted and retaining the original document formatting.";

        public GeminiApiClient(string apiKey, ILogger logger)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _logger.LogInformation("Gemini API client initialized");
        }

        public async Task<string> ExtractTextFromImageAsync(Bitmap image, string prompt = DEFAULT_PROMPT)
        {
            try
            {
                _logger.LogInformation("Preparing image for Gemini API");

                // Convert image to base64
                string base64Image = ConvertImageToBase64(image);

                // Build request
                var requestUrl = $"{API_URL}?key={_apiKey}";

                // Build request với cấu trúc output như ví dụ
                var requestBody = new JObject(
                    new JProperty("contents", new JArray(
                        new JObject(
                            new JProperty("role", "user"),
                            new JProperty("parts", new JArray(
                                new JObject(
                                    new JProperty("text", prompt)
                                ),
                                new JObject(
                                    new JProperty("inline_data", new JObject(
                                        new JProperty("mime_type", "image/jpeg"),
                                        new JProperty("data", base64Image)
                                    ))
                                )
                            ))
                        )
                    )),
                    new JProperty("generationConfig", new JObject(
                        new JProperty("temperature", 0.1),
                        new JProperty("topK", 40),
                        new JProperty("topP", 0.95),
                        new JProperty("maxOutputTokens", 8192),
                        new JProperty("responseMimeType", "application/json"),
                        new JProperty("responseSchema", new JObject(
                            new JProperty("type", "object"),
                            new JProperty("properties", new JObject(
                                new JProperty("text", new JObject(
                                    new JProperty("type", "string")
                                ))
                            ))
                        ))
                    ))
                );


                _logger.LogDebug("Sending request to Gemini API");

                // Send request
                var response = await _httpClient.PostAsync(
                    requestUrl,
                    new StringContent(requestBody.ToString(), Encoding.UTF8, "application/json")
                );

                // Process response
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("Received successful response from Gemini API");

                    // Parse the response JSON
                    var jsonResponse = JObject.Parse(responseContent);

                    // Extract the text content from the response
                    var contentText = jsonResponse["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

                    if (string.IsNullOrEmpty(contentText))
                    {
                        _logger.LogWarning("No text content found in Gemini API response");
                        return "No text was found in the image.";
                    }

                    _logger.LogInformation("Successfully extracted text from image");
                    return contentText;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API request failed: {response.StatusCode}. Response: {errorContent}");
                    return $"Error: {response.StatusCode} - {errorContent}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                return $"Error: {ex.Message}";
            }
        }

        private string ConvertImageToBase64(Bitmap image)
        {
            using var ms = new MemoryStream();
            // Save as JPEG for better compatibility
            image.Save(ms, ImageFormat.Jpeg);
            byte[] imageBytes = ms.ToArray();
            return Convert.ToBase64String(imageBytes);
        }
    }
}
