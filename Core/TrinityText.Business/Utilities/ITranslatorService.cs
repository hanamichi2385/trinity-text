using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface ITranslatorService
    {
        Task<string> TranslateText(string text, string sourceLang, string targetLang);
    }
}
