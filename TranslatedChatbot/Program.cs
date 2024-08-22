using Azure;
using Azure.AI.Language.QuestionAnswering;
using Newtonsoft.Json;
using System.Text;

namespace TranslatedChatbot

// Emma Lind, .NET23
// Labb 1 - Natural Language Processing och frågetjänster i Azure AI
// This is a c(h)atbot with a translator that can be asked questions about cats. 
// QnA language service and translate service is used in Azure
{
    internal class Program
    {
        // For Translate service
        private static readonly string translateKey = "a3c0a14ade0e47eea8c5a42a05bbd9b0";
        private static readonly string translateEndpoint = "https://api.cognitive.microsofttranslator.com";
        private static readonly string translateLocation = "westeurope";

        static async Task Main(string[] args)
        {
            // For QnA
            Uri qnaEndpoint = new Uri("https://ailanguagesmodelemmalind.cognitiveservices.azure.com/");
            AzureKeyCredential qnaCredential = new AzureKeyCredential("978fa3862e5f406ca4f9a3048d66c8ec");
            string projectName = "FAQLabb1";
            string deploymentName = "production";

            QuestionAnsweringClient client = new QuestionAnsweringClient(qnaEndpoint, qnaCredential);
            QuestionAnsweringProject project = new QuestionAnsweringProject(projectName, deploymentName);

            Console.ForegroundColor
            = ConsoleColor.Yellow;
            Console.WriteLine(" _._     _,-'\"\"`-._\r\n(,-.`._,'(       |\\`-/|\r\n    `-.-' \\ )-`( , o o)\r\n          `-    \\`_`\"'-");
            Console.ForegroundColor
            = ConsoleColor.Cyan;
            Console.WriteLine("****** Welcome to CatBot! *******\n");
            Console.ForegroundColor
            = ConsoleColor.White;
            Console.WriteLine("Ask your cat-questions in any language!\nType 'exit' to quit");

            while (true)
            {
                Console.Write("\nQ: ");
                string question = Console.ReadLine(); //ask catbot a question
                if (question.ToLower() == "exit") //if user types exit, quit program
                {
                    Console.ForegroundColor
                    = ConsoleColor.Yellow;
                    Console.WriteLine("\n      |\\      _,,,---,,_\r\nZZZzz /,`.-'`'    -.  ;-;;,_\r\n     |,4-  ) )-,_. ,\\ (  `'-'\r\n    '---''(_/--'  `-'\\_)           ");
                    Console.ForegroundColor
                    = ConsoleColor.Cyan;
                    Console.WriteLine("\n   Thank you for using CatBot! ");
                    Console.ForegroundColor
                    = ConsoleColor.White;

                    break;
                }

                // Detects the language of user input
                string detectedLanguage = await DetectLanguageAsync(translateKey, translateEndpoint, translateLocation, question);

                // Translates user input to english
                string translatedQuestion = await TranslateTextAsync(translateKey, translateEndpoint, translateLocation, question, "en");

                try
                {
                    // The translated question is sent to QnA-Service and answer is collected
                    Response<AnswersResult> response = client.GetAnswers(translatedQuestion, project);
                    foreach (KnowledgeBaseAnswer answer in response.Value.Answers)
                    {
                        // Translates the english answer to the users input language 
                        string translatedAnswer = await TranslateTextAsync(translateKey, translateEndpoint, translateLocation, answer.Answer, detectedLanguage);
                        Console.WriteLine($"A: {translatedAnswer}"); // prints answer from catbot
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Request error: " + ex.Message);
                }
            }
        }

        // Azure translate - translates text
        public static async Task<string> TranslateTextAsync(string subscriptionKey, string endpoint, string location, string text, string targetLanguage)
        {
            string route = $"/translate?api-version=3.0&to={targetLanguage}"; // define route for api request, adding targetlanguage

            // Create the request body
            object[] body = new object[] { new { Text = text } };
            var requestBody = JsonConvert.SerializeObject(body);

            // Create an HTTP client and request
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post; // post method
                request.RequestUri = new Uri(endpoint + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey); // add key to request header
                request.Headers.Add("Ocp-Apim-Subscription-Region", location); // add location to request header

                // send request and await response
                HttpResponseMessage response = await client.SendAsync(request);
                string result = await response.Content.ReadAsStringAsync();

                // extract translated text from json
                var translatedText = JsonConvert.DeserializeObject<dynamic>(result)[0].translations[0].text;
                return translatedText; // Return the translated text
            }
        }

        // Azure Translate - Detects the language of the input text
        public static async Task<string> DetectLanguageAsync(string subscriptionKey, string endpoint, string location, string text)
        {
            // Define the route for the language detection API
            string route = "/detect?api-version=3.0";

            // Create the request body
            object[] body = new object[] { new { Text = text } };
            var requestBody = JsonConvert.SerializeObject(body);

            // Create an HTTP client and request
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post; // set method to post
                request.RequestUri = new Uri(endpoint + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey); // add key to request headers
                request.Headers.Add("Ocp-Apim-Subscription-Region", location); // add location to request header

                //send the request and wait for response
                HttpResponseMessage response = await client.SendAsync(request);
                string result = await response.Content.ReadAsStringAsync(); // read response as string

                var detectedLanguage = JsonConvert.DeserializeObject<dynamic>(result)[0].language;
                return detectedLanguage; // return detected language code
            }
        }
    }
}
