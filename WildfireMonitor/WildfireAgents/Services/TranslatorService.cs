using Azure;
using Azure.AI.Translation.Text;
using Microsoft.Extensions.Logging;

namespace WildfireAgents.Services
{
    public class TranslatorService
    {
        private readonly ILogger<TranslatorService> _logger;
        private readonly TextTranslationClient _textTranslationClient;
        public TranslatorService(
            ILogger<TranslatorService> logger,
            TextTranslationClient textTranslationClient)
        {
            _logger = logger;
            _textTranslationClient = textTranslationClient;
        }

        public async Task<string?> TranslateAsync(string text, string targetLanguage)
        {
            try
            {
                Response<IReadOnlyList<TranslatedTextItem>> response = await _textTranslationClient.TranslateAsync(targetLanguage, text).ConfigureAwait(false);
                IReadOnlyList<TranslatedTextItem> translations = response.Value;
                TranslatedTextItem translation = translations.FirstOrDefault() ?? throw new ArgumentNullException("No translation was found.");
                return translation?.Translations?.FirstOrDefault()?.Text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while translating text with Text Translation Service.");
                throw;
            }
        }
    }
}
