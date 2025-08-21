namespace HRM.Interface
{
    public interface IJsonLocalizationService
    {
        string Translate(string key);
        void SetLanguage(string culture);
        string CurrentLanguage { get; }
    }

}
