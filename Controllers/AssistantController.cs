using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagementSystem.Web.Services;
using SchoolManagementSystem.Web.ViewModels.Assistant;

namespace SchoolManagementSystem.Web.Controllers;

[Authorize]
public class AssistantController : Controller
{
    private readonly AiAssistantService _assistantService;

    public AssistantController(AiAssistantService assistantService)
    {
        _assistantService = assistantService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask([FromBody] ChatRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            return Json(new ChatResponse
            {
                Success = false,
                Reply = LocalizeValidationMessage("empty", DetectUiLanguage())
            });
        }

        if (request.Message.Length > 1000)
        {
            return Json(new ChatResponse
            {
                Success = false,
                Reply = LocalizeValidationMessage("tooLong", DetectUiLanguage())
            });
        }

        var userMessage = request.Message.Trim();
        var responseLanguage = DetectResponseLanguage(userMessage);
        var messageForAssistant = AddLanguageInstruction(userMessage, responseLanguage);

        var (reply, navigateUrl) = await _assistantService.AskAsync(
            User,
            messageForAssistant,
            request.History ?? new List<ChatHistoryItem>());

        reply = LocalizeKnownServiceReply(reply, responseLanguage);

        return Json(new ChatResponse
        {
            Success = true,
            Reply = reply,
            NavigateUrl = navigateUrl
        });
    }

    private static string AddLanguageInstruction(string message, string language)
    {
        var instruction = language switch
        {
            "tg" =>
                "[ҚОИДАИ ҶАВОБ: Ин савол ба забони тоҷикӣ аст. " +
                "Фақат ба забони тоҷикӣ ҷавоб деҳ. Забони русии маълумоти порталро " +
                "ҳамчун забони ҷавоб интихоб накун.]",
            "ru" =>
                "[ПРАВИЛО ОТВЕТА: Этот вопрос задан на русском языке. " +
                "Отвечай только на русском. Не меняй язык ответа из-за того, " +
                "что данные портала могут быть написаны на другом языке.]",
            _ =>
                "[RESPONSE LANGUAGE RULE: This question is in English. " +
                "Answer only in English. Do not switch to Russian just because " +
                "the portal data or system context is written in Russian.]"
        };

        return $"{message}\n\n{instruction}";
    }

    private static string DetectResponseLanguage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return DetectUiLanguage();
        }

        if (Regex.IsMatch(message, "[ӣғқӯҳҷӢҒҚӮҲҶ]"))
        {
            return "tg";
        }

        var lower = message.ToLowerInvariant();

        var tajikCyrillicWords = new[]
        {
            "чанд", "аст", "ҳаст", "баҳо", "баҳои", "давомат", "маълумот",
            "хонанда", "омӯзгор", "муаллим", "дарс", "ҷадвал", "имрӯз", "фардо",
            "волид", "фарзанд", "гурӯҳ", "куҷо", "чӣ", "чист", "кист", "барои",
            "лутфан", "мехоҳам", "метавонам", "нишон деҳ"
        };

        if (tajikCyrillicWords.Any(word => ContainsWordOrPhrase(lower, word)))
        {
            return "tg";
        }

        if (Regex.IsMatch(message, "[А-Яа-яЁё]"))
        {
            return "ru";
        }

        var latinTokens = Regex
            .Split(lower, "[^a-z']+")
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .ToArray();

        var tajikLatinStems = new[]
        {
            "davom", "baho", "malumot", "malumat", "malamut", "ma'lumot",
            "khonand", "xonand", "omuz", "muallim", "jadval", "imruz", "fardo",
            "guruh", "guruhi", "kujo", "chand", "chist", "kist", "farzand",
            "volid", "talaba", "darsi", "darsho"
        };

        if (latinTokens.Any(token =>
                tajikLatinStems.Any(stem => token.StartsWith(stem, StringComparison.Ordinal))))
        {
            return "tg";
        }

        var weakTajikLatinWords = new HashSet<string>(StringComparer.Ordinal)
        {
            "ast", "hast", "chi", "baroi", "lutfan", "mekhoham", "metavonam",
            "man", "dar", "az", "ba"
        };

        if (latinTokens.Count(token => weakTajikLatinWords.Contains(token)) >= 2)
        {
            return "tg";
        }

        if (Regex.IsMatch(message, "[A-Za-z]"))
        {
            return "en";
        }

        return DetectUiLanguage();
    }

    private static bool ContainsWordOrPhrase(string text, string value)
    {
        if (value.Contains(' '))
        {
            return text.Contains(value, StringComparison.Ordinal);
        }

        return Regex.IsMatch(
            text,
            $@"(?<![\p{{L}}]){Regex.Escape(value)}(?![\p{{L}}])",
            RegexOptions.CultureInvariant);
    }

    private static string DetectUiLanguage()
    {
        var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        return language.Equals("ru", StringComparison.OrdinalIgnoreCase)
            ? "ru"
            : language.Equals("tg", StringComparison.OrdinalIgnoreCase)
                ? "tg"
                : "en";
    }

    private static string LocalizeValidationMessage(string key, string language)
    {
        return (key, language) switch
        {
            ("empty", "tg") => "Саволро нависед.",
            ("empty", "en") => "Enter a message.",
            ("tooLong", "tg") => "Паём хеле дароз аст.",
            ("tooLong", "en") => "The message is too long.",
            ("tooLong", _) => "Сообщение слишком длинное.",
            _ => "Введите сообщение."
        };
    }

    private static string LocalizeKnownServiceReply(string reply, string language)
    {
        return reply switch
        {
            "Ассистент пока не настроен: не указан ключ Gemini API." => language switch
            {
                "tg" => "Ёрдамчӣ ҳоло танзим нашудааст: калиди Gemini API нишон дода нашудааст.",
                "en" => "The assistant is not configured yet: the Gemini API key is missing.",
                _ => reply
            },
            "Не удалось получить ответ от ассистента. Попробуйте ещё раз чуть позже." => language switch
            {
                "tg" => "Ҷавоб аз ёрдамчӣ гирифта нашуд. Лутфан, каме дертар боз кӯшиш кунед.",
                "en" => "The assistant could not return an answer. Please try again a little later.",
                _ => reply
            },
            "Не удалось получить ответ от ассистента. Попробуйте переформулировать вопрос." => language switch
            {
                "tg" => "Ҷавоб аз ёрдамчӣ гирифта нашуд. Лутфан, саволро дигар хел нависед.",
                "en" => "The assistant could not return an answer. Please rephrase the question.",
                _ => reply
            },
            "Не удалось связаться с ассистентом. Проверьте подключение и попробуйте снова." => language switch
            {
                "tg" => "Ба ёрдамчӣ пайваст шудан муяссар нашуд. Пайвастро санҷед ва боз кӯшиш кунед.",
                "en" => "Could not connect to the assistant. Check your connection and try again.",
                _ => reply
            },
            _ => reply
        };
    }
}
