using Newtonsoft.Json.Linq;

class DuckDuckGoSearchService
{
    private static readonly HttpClient httpClient = new HttpClient();

    public async Task<string> SearchDuckDuckGoAsync(string query)
    {
        try
        {
            // Формируем запрос к DuckDuckGo API с параметром для русского языка
            string apiUrl = $"https://api.duckduckgo.com/?q={Uri.EscapeDataString(query)}&format=json&pretty=1&kl=ru-ru";

            // Отправляем GET-запрос
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                // Получаем JSON-ответ
                string jsonResponse = await response.Content.ReadAsStringAsync();

                // Парсим JSON-ответ
                JObject json = JObject.Parse(jsonResponse);

                // Извлекаем основные данные из JSON-ответа
                string abstractText = json["AbstractText"]?.ToString();
                string answerText = json["Answer"]?.ToString();
                JArray relatedTopics = (JArray)json["RelatedTopics"];

                // Если ничего полезного не найдено
                if (string.IsNullOrEmpty(abstractText) && string.IsNullOrEmpty(answerText) && (relatedTopics == null || relatedTopics.Count == 0))
                {
                    return "Ничего не найдено по вашему запросу. Пожалуйста, задавайте вопросы только на темы, связанные с обучением.";
                }

                // Собираем результат
                string result = "Результаты поиска:\n";

                if (!string.IsNullOrEmpty(abstractText))
                {
                    result += $"Описание: {abstractText}\n";
                }
                if (!string.IsNullOrEmpty(answerText))
                {
                    result += $"Ответ: {answerText}\n";
                }
                if (relatedTopics != null && relatedTopics.Count > 0)
                {
                    result += "Связанные темы:\n";
                    foreach (var topic in relatedTopics)
                    {
                        string topicText = topic["Text"]?.ToString();
                        string topicUrl = topic["FirstURL"]?.ToString();
                        result += $"- {topicText}: {topicUrl}\n";
                    }
                }

                return result;
            }
            else
            {
                return "Ошибка при поиске.";
            }
        }
        catch (Exception ex)
        {
            return $"Ошибка: {ex.Message}";
        }
    }
}