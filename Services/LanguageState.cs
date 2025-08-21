namespace HRM.Services
{
   
    public class LanguageState
    {
        public event Func<Task>? OnLanguageChangedAsync;

        public async Task NotifyAsync()
        {
            if (OnLanguageChangedAsync != null)
            {
                await OnLanguageChangedAsync.Invoke();
            }
        }
    }

}
